using UnityEngine;

namespace LostRealms
{
    /// Aster, ported from GameView.java: run/jump/double-jump/coyote/buffer, 4-stage sword combo,
    /// air-attack bounce, hurt invulnerability, checkpoint respawn. Custom AABB physics (no
    /// Rigidbody) against LevelRegistry one-way platforms — identical logic to the original.
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Tooltip("Model child; mirrored on facing change. Animator (if any) is driven automatically.")]
        public Transform visualRoot;

        public Vector2 Velocity => new Vector2(_vx, _vy);
        public bool Grounded => _grounded;
        public bool FacingLeft => _facingLeft;
        public int AttackStage => _attackStage;
        public bool Attacking => _attackTime > 0f;

        private float _vx, _vy;
        private bool _grounded, _canDouble, _facingLeft, _jumpHeld;
        private float _coyote, _jumpBuffer;
        private int _attackStage;
        private float _attackTime, _comboWindow, _attackHitDoneId;
        private bool _airAttackUsed;
        private float _invuln, _dead;
        private float _bobSeed;
        private GameObject _slashFx;
        private float _slashTime;

        private Animator _animator;
        private static readonly int ASpeed = Animator.StringToHash("Speed");
        private static readonly int AGrounded = Animator.StringToHash("Grounded");
        private static readonly int AVertical = Animator.StringToHash("VerticalVelocity");
        private static readonly int AAttack = Animator.StringToHash("Attack");
        private static readonly int AHurt = Animator.StringToHash("Hurt");
        private static readonly int ADeath = Animator.StringToHash("Death");

        private void Awake()
        {
            Instance = this;
            _animator = GetComponentInChildren<Animator>();
            _bobSeed = Random.value * 6f;
        }

        private void Update()
        {
            if (_dead > 0f)
            {
                _dead -= Time.deltaTime;
                if (_dead <= 0f) Respawn();
                return;
            }

            float dt = Time.deltaTime;
            var input = TouchControls.State;

            // ----- horizontal
            float target = 0f;
            if (input.moveX < -0.25f) { target = -GameConfig.RunSpeed; _facingLeft = true; }
            else if (input.moveX > 0.25f) { target = GameConfig.RunSpeed; _facingLeft = false; }
            float accel = _grounded ? GameConfig.GroundAccel : GameConfig.AirAccel;
            _vx = Mathf.MoveTowards(_vx, target, accel * dt);

            // ----- jump buffering / coyote (GameView 314-332)
            if (input.jumpPressed) _jumpBuffer = GameConfig.JumpBufferTime;
            else if (_jumpBuffer > 0f) _jumpBuffer -= dt;
            _jumpHeld = input.jumpHeld;
            if (_jumpBuffer > 0f && TryJump()) _jumpBuffer = 0f;

            // ----- gravity (variable like original)
            float gravity = GameConfig.Gravity;
            if (!_jumpHeld && _vy < -GameConfig.FallGravityThreshold) gravity *= GameConfig.FallGravityMultiplier;
            _vy -= gravity * dt;
            if (_vy < -GameConfig.MaxFallSpeed) _vy = -GameConfig.MaxFallSpeed;

            // ----- attack
            if (input.attackPressed)
            {
                if (!_grounded && !_airAttackUsed) BeginAirAttack();
                else if (_attackTime <= 0f) BeginAttack(1);
                else if (_comboWindow > 0f && _attackStage < 4) BeginAttack(_attackStage + 1);
            }
            if (_attackTime > 0f)
            {
                _attackTime -= dt;
                ResolveAttackHits();
            }
            if (_comboWindow > 0f) _comboWindow -= dt;
            else if (_attackTime <= 0f) _attackStage = 0;

            // ----- integrate + one-way platform landing
            float prevBottom = transform.position.y;
            float newY = prevBottom + _vy * dt;
            float? landed = LevelRegistry.LandOnPlatform(GameConfig.HeroHalfWidth, transform.position.x, prevBottom, newY);
            if (landed.HasValue)
            {
                newY = landed.Value;
                _vy = 0f;
                if (!_grounded) _airAttackUsed = false;
                _grounded = true;
                _canDouble = true;
                _coyote = GameConfig.CoyoteTime;
            }
            else
            {
                if (_grounded)
                {
                    _coyote = GameConfig.CoyoteTime;   // fresh off the edge
                    _grounded = false;
                }
                else if (_coyote > 0f) _coyote -= dt;
            }
            transform.position = new Vector3(transform.position.x + _vx * dt, newY, 0f);

            // ----- world interactions
            CheckPickupsAndEnemies(dt);
            CheckHazardsAndPits();

            if (_invuln > 0f) _invuln -= dt;

            UpdateVisuals(dt);
            DriveAnimator();
        }

        private bool TryJump()
        {
            if (_grounded || _coyote > 0f)
            {
                _vy = GameConfig.JumpVelocity;
                _grounded = false;
                _coyote = 0f;
                _canDouble = true;
                return true;
            }
            if (_canDouble)
            {
                _vy = GameConfig.DoubleJumpVelocity;
                _canDouble = false;
                return true;
            }
            return false;
        }

        private void BeginAttack(int stage)
        {
            _attackStage = stage;
            _attackTime = GameConfig.AttackDuration;
            _comboWindow = GameConfig.ComboWindow;
            _attackHitDoneId++;
            SpawnSlashFx();
            if (_animator != null) _animator.SetTrigger(AAttack);
        }

        private void BeginAirAttack()
        {
            _airAttackUsed = true;
            _attackStage = 1;
            _attackTime = 0.2f;
            _attackHitDoneId++;
            SpawnSlashFx();
            if (_animator != null) _animator.SetTrigger(AAttack);
        }

        private void ResolveAttackHits()
        {
            // hit frame fires once, 60% into the wind-up (AttackHitAt from config)
            bool hitFrame = _attackTime <= GameConfig.AttackDuration - GameConfig.AttackHitAt;
            if (!hitFrame) return;
            float reach = GameConfig.ComboReach[Mathf.Clamp(_attackStage, 1, 4) - 1];
            int damage = GameConfig.ComboDamage[Mathf.Clamp(_attackStage, 1, 4) - 1];
            float dir = _facingLeft ? -1f : 1f;
            float minX = _facingLeft ? transform.position.x - reach : transform.position.x;
            float maxX = minX + reach;
            float y = transform.position.y + GameConfig.HeroHeight * 0.6f;

            bool anyHit = Enemy.TryDamageAll(minX, maxX, y - GameConfig.AttackVerticalReach, y + GameConfig.AttackVerticalReach, damage, _attackHitDoneId);
            if (anyHit && !_grounded)
            {
                _vy = GameConfig.AirAttackBounce;      // pogo bounce (GameView 633)
                _canDouble = true;
                _airAttackUsed = false;
            }
        }

        private void CheckPickupsAndEnemies(float dt)
        {
            Pickup.CheckCollectAll(transform.position);
            Enemy.CheckContactAll(transform.position, GameConfig.HeroHalfWidth, GameConfig.HeroHeight, dt, this);
        }

        private void CheckHazardsAndPits()
        {
            float y = transform.position.y;
            if (LevelRegistry.OverlapsHazard(
                transform.position.x - GameConfig.HeroHalfWidth * 0.6f,
                transform.position.x + GameConfig.HeroHalfWidth * 0.6f,
                y + 0.05f, y + GameConfig.HeroHeight * 0.7f))
            {
                TakeDamage(1, transform.position.x + (_facingLeft ? -1f : 1f));
                return;
            }
            if (y < GameConfig.KillPlaneY) Die();
            if (transform.position.x >= GameConfig.LevelEndX && GameManager.Instance != null)
                GameManager.Instance.LevelComplete();
        }

        public void TakeDamage(int amount, float fromX)
        {
            if (_invuln > 0f || _dead > 0f) return;
            if (GameManager.Instance != null) GameManager.Instance.OnPlayerHurt(amount);
            _vx = (transform.position.x < fromX ? -1f : 1f) * GameConfig.HitKnockback;
            _vy = 4.2f;
            _invuln = GameConfig.HurtInvuln;
            if (_animator != null) _animator.SetTrigger(AHurt);
            if (GameManager.Instance != null && GameManager.Instance.Health <= 0) Die();
        }

        private void Die()
        {
            if (_dead > 0f) return;
            _dead = 1.3f;
            _vx = _vy = 0f;
            if (_animator != null) _animator.SetTrigger(ADeath);
            if (GameManager.Instance != null) GameManager.Instance.OnPlayerDied();
        }

        private void Respawn()
        {
            transform.position = new Vector3(LevelRegistry.Checkpoint.x, LevelRegistry.Checkpoint.y, 0f);
            _vx = _vy = 0f;
            _invuln = GameConfig.RespawnInvuln;
            _attackStage = 0;
            _attackTime = 0f;
            if (GameManager.Instance != null) GameManager.Instance.OnRespawn();
        }

        // ------------------------------------------------ visuals (placeholder motion until Mixamo rig)

        private void UpdateVisuals(float dt)
        {
            if (visualRoot == null) return;
            float dir = _facingLeft ? -1f : 1f;
            Vector3 scale = visualRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * dir;
            visualRoot.localScale = scale;

            float speed01 = Mathf.Abs(_vx) / GameConfig.RunSpeed;
            float tilt = 0f, bob = 0f;
            if (_grounded)
            {
                tilt = -6f * speed01 * dir;
                bob = Mathf.Abs(Mathf.Sin(Time.time * 11f + _bobSeed)) * 0.05f * speed01;
            }
            else
            {
                tilt = Mathf.Clamp(-_vy * 1.6f, -14f, 12f) * dir * -1f;
            }
            visualRoot.localRotation = Quaternion.Euler(0f, 90f, tilt);
            visualRoot.localPosition = new Vector3(visualRoot.localPosition.x, bob, visualRoot.localPosition.z);

            // hurt flicker
            bool flicker = _invuln > 0f && Mathf.PingPong(Time.time * 14f, 1f) > 0.5f;
            SetRenderersEnabled(!flicker || _dead > 0f);

            if (_slashTime > 0f)
            {
                _slashTime -= dt;
                if (_slashFx != null)
                {
                    float t = 1f - _slashTime / 0.14f;
                    _slashFx.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(50f, -40f, t) * dir);
                    var c = _slashFx.GetComponent<MeshRenderer>().material.color;
                    c.a = 1f - t;
                    _slashFx.GetComponent<MeshRenderer>().material.color = c;
                    if (_slashTime <= 0f) _slashFx.SetActive(false);
                }
            }
        }

        private void SetRenderersEnabled(bool on)
        {
            foreach (var r in GetComponentsInChildren<MeshRenderer>())
                if (r.gameObject != _slashFx) r.enabled = on;
        }

        private void SpawnSlashFx()
        {
            if (_slashFx == null)
            {
                _slashFx = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(_slashFx.GetComponent<Collider>());
                _slashFx.name = "SlashFx";
                _slashFx.transform.SetParent(transform, false);
                _slashFx.transform.localScale = new Vector3(0.08f, 1.15f, 0.08f);
                _slashFx.transform.localPosition = new Vector3(0.55f, GameConfig.HeroHeight * 0.62f, -0.15f);
                var m = new Material(Shader.Find("Sprites/Default"));
                m.color = new Color(1f, 0.95f, 0.75f, 0.85f);
                _slashFx.GetComponent<MeshRenderer>().material = m;
                _slashFx.SetActive(false);
            }
            _slashFx.SetActive(true);
            _slashTime = 0.14f;
            _slashFx.transform.localPosition = new Vector3(_facingLeft ? -0.55f : 0.55f, GameConfig.HeroHeight * 0.62f, -0.15f);
        }

        private void DriveAnimator()
        {
            if (_animator == null) return;
            _animator.SetFloat(ASpeed, Mathf.Abs(_vx));
            _animator.SetBool(AGrounded, _grounded);
            _animator.SetFloat(AVertical, _vy);
        }
    }
}
