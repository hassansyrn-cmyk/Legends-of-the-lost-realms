using System.Collections.Generic;
using UnityEngine;

namespace LostRealms
{
    /// Enemy FSM ported from EnemyController.java archetypes + GameView update logic:
    /// PATROL -> NOTICE -> WINDUP -> ATTACK (committed contact damage) -> RECOVERY -> PATROL,
    /// plus HIT_REACTION on sword hits. Ground crawlers walk their platform; flyers dive.
    public class Enemy : MonoBehaviour
    {
        public struct Archetype
        {
            public string name; public int maxHealth; public float patrolSpeed; public Color accent;
            public float noticeRange, noticeHeight, windup; public int contactDamage; public bool committed;
            public float noticeTime, attackTime, recoveryTime;
        }

        internal static readonly Archetype[] Archetypes = NewArchetypes();

        private static Archetype[] NewArchetypes()
        {
            var a = new Archetype[8];
            a[0] = Make("Moss Mask Crawler", 2, 0.30f, 0xFFBE68, 1.75f, 0.88f, 0.34f, 1, true, 0.16f, 0.30f, 0.40f);
            a[1] = Make("Ember Moth",        2, 0.42f, 0xFF8F62, 2.35f, 1.35f, 0.42f, 1, true, 0.18f, 0.42f, 0.46f);
            a[2] = Make("Dune Skirmisher",   2, 0.54f, 0xFFB246, 2.10f, 0.90f, 0.46f, 1, true, 0.12f, 0.34f, 0.28f);
            a[3] = Make("Frost Sentinel",    2, 0.66f, 0x8DE8FF, 1.50f, 0.95f, 0.58f, 2, false, 0.22f, 0.24f, 0.62f);
            a[4] = Make("Wind Wisp",         4, 0.78f, 0xC099FF, 2.60f, 1.25f, 0.38f, 1, true, 0.14f, 0.30f, 0.34f);
            a[5] = Make("Aegis Guard",       3, 0.26f, 0x69D9D1, 1.80f, 0.90f, 0.52f, 1, true, 0.20f, 0.32f, 0.48f);
            a[6] = Make("Stone Brute",       5, 0.18f, 0xFF826E, 2.05f, 1.00f, 0.72f, 2, true, 0.24f, 0.30f, 0.72f);
            a[7] = Make("Rune Caster",       3, 0.00f, 0xB5A1FF, 3.15f, 1.65f, 0.66f, 1, false, 0.20f, 0.18f, 0.64f);
            return a;
        }

        private static Archetype Make(string name, int hp, float speed, int accentHex, float range, float height,
            float windup, int dmg, bool committed, float notice, float attack, float recovery)
        {
            ColorUtility.TryParseHtmlString($"#{accentHex:X6}", out var accent);
            return new Archetype
            {
                name = name, maxHealth = hp, patrolSpeed = speed, accent = accent,
                noticeRange = range, noticeHeight = height, windup = windup,
                contactDamage = dmg, committed = committed,
                noticeTime = notice, attackTime = attack, recoveryTime = recovery
            };
        }

        internal const int PATROL = 0, NOTICE = 1, WINDUP = 2, ATTACK = 3, RECOVERY = 4, HIT = 5;

        private static readonly List<Enemy> All = new List<Enemy>();

        private Archetype _arch;
        private int _kind, _hp, _state;
        private float _stateTime, _dir = 1f, _minX, _maxX, _homeY;
        private float _hurtTime, _hitLock, _dieTime = -1f;
        private Vector2 _attackDir;
        private bool _flying => _kind == 1 || _kind == 4;
        private Transform _wings;
        private Animator _animator;

        public static Enemy Create(Vector2 pos, int kind)
        {
            var go = new GameObject($"Foe_{kind}");
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var e = go.AddComponent<Enemy>();
            e.Init(kind, pos);
            return e;
        }

        private void Init(int kind, Vector2 pos)
        {
            _kind = Mathf.Clamp(kind, 0, Archetypes.Length - 1);
            _arch = Archetypes[_kind];
            _hp = _arch.maxHealth;
            _minX = pos.x - 1.2f;
            _maxX = pos.x + 1.2f;
            _homeY = pos.y;
            All.Add(this);
            BuildVisual();
        }

        private void BuildVisual()
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.name = "Body";
            body.transform.SetParent(transform, false);
            body.GetComponent<MeshRenderer>().material = SolidMat(_arch.accent);
            _animator = null;   // replaced by rigged models in a later pass

            if (_flying)
            {
                body.transform.localScale = new Vector3(0.55f, 0.75f, 0.55f);
                _wings = new GameObject("Wings").transform;
                _wings.SetParent(transform, false);
                _wings.localPosition = new Vector3(0f, 0.15f, 0f);
                for (int i = -1; i <= 1; i += 2)
                {
                    var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(wing.GetComponent<Collider>());
                    wing.transform.SetParent(_wings, false);
                    wing.transform.localPosition = new Vector3(i * 0.45f, 0.1f, 0f);
                    wing.transform.localScale = new Vector3(0.7f, 0.08f, 0.45f);
                    wing.GetComponent<MeshRenderer>().material = SolidMat(Color.Lerp(_arch.accent, Color.white, 0.4f));
                }
            }
            else
            {
                body.transform.localScale = new Vector3(0.85f, 0.62f, 0.85f);
                body.transform.localPosition = new Vector3(0f, 0.42f, 0f);
                var mask = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(mask.GetComponent<Collider>());
                mask.name = "Mask";
                mask.transform.SetParent(transform, false);
                mask.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                mask.transform.localScale = new Vector3(0.7f, 0.28f, 0.5f);
                mask.GetComponent<MeshRenderer>().material = SolidMat(Color.Lerp(_arch.accent, Color.black, 0.55f));
            }
        }

        private static Material SolidMat(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (m == null || m.shader == null || !m.shader.isSupported)
                m = new Material(Shader.Find("Standard"));
            m.color = c;
            return m;
        }

        private void OnDestroy() { All.Remove(this); }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (_dieTime >= 0f)
            {
                _dieTime -= dt;
                transform.localScale = Vector3.one * Mathf.Max(0.01f, _dieTime / 0.35f);
                if (_dieTime <= 0f) { SpawnDeathBurst(); Destroy(gameObject); }
                return;
            }

            if (_hitLock > 0f) { _hitLock -= dt; return; }
            if (_hurtTime > 0f) _hurtTime -= dt;

            var player = PlayerController.Instance;
            float px = player != null ? player.transform.position.x : 0f;
            float py = player != null ? player.transform.position.y : 0f;
            float dx = px - transform.position.x, dy = py - transform.position.y;

            _stateTime -= dt;
            switch (_state)
            {
                case PATROL:
                    float x = transform.position.x + _dir * _arch.patrolSpeed * dt;
                    if (x < _minX || x > _maxX) _dir *= -1f;
                    SetX(x);
                    if (!_flying) GroundSnap();
                    else FlyBob(dt);
                    if (CanNotice(Mathf.Abs(dx), Mathf.Abs(dy)))
                    {
                        _state = NOTICE; _stateTime = _arch.noticeTime; _dir = dx < 0 ? -1 : 1;
                    }
                    break;

                case NOTICE:
                    if (_stateTime <= 0f) { _state = WINDUP; _stateTime = _arch.windup; }
                    break;

                case WINDUP:
                    if (_stateTime <= 0f)
                    {
                        _state = ATTACK; _stateTime = _arch.attackTime;
                        _attackDir = Vector2.right * Mathf.Sign(dx == 0 ? _dir : dx);
                        if (_flying) _attackDir = new Vector2(dx, dy).normalized;
                    }
                    break;

                case ATTACK:
                    if (_flying) transform.position += (Vector3)(_attackDir * 2.1f * dt);
                    else SetX(transform.position.x + _attackDir.x * 2.45f * dt);
                    if (!_flying) GroundSnap();
                    if (_stateTime <= 0f) { _state = RECOVERY; _stateTime = _arch.recoveryTime; }
                    break;

                case RECOVERY:
                    if (!_flying) GroundSnap();
                    else FlyBob(dt);
                    if (_stateTime <= 0f) _state = PATROL;
                    break;

                case HIT:
                    if (!_flying) GroundSnap();
                    if (_stateTime <= 0f) _state = PATROL;
                    break;
            }

            // wings flutter
            if (_wings != null)
                _wings.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 22f) * 40f);

            // hurt tint pulse
            float scaleFix = _hurtTime > 0f ? 1.12f : 1f;
            transform.localScale = Vector3.one * scaleFix * 0.999f;
        }

        private bool CanNotice(float adx, float ady) => adx < _arch.noticeRange && ady < _arch.noticeHeight;

        private void SetX(float x) => transform.position = new Vector3(Mathf.Clamp(x, _minX - 0.5f, _maxX + 0.5f), transform.position.y, 0f);

        private void GroundSnap()
        {
            // stand on the highest platform top below the foe's feet
            float best = GameConfig.KillPlaneY;
            float feet = transform.position.y + 0.05f;
            foreach (var p in LevelRegistry.Platforms)
            {
                if (transform.position.x < p.Left || transform.position.x > p.Right) continue;
                if (p.Top <= feet + 0.4f && p.Top > best) best = p.Top;
            }
            if (best > GameConfig.KillPlaneY + 0.1f)
                transform.position = new Vector3(transform.position.x, Mathf.Lerp(transform.position.y, best, 0.5f), 0f);
        }

        private void FlyBob(float dt)
        {
            var p = transform.position;
            p.y = Mathf.Lerp(p.y, _homeY + Mathf.Sin(Time.time * 2.4f) * 0.25f, dt * 2f);
            transform.position = p;
        }

        // ---------------------------------------------------------------- damage interface

        internal static bool TryDamageAll(float minX, float maxX, float minY, float maxY, int damage, int hitId)
        {
            bool any = false;
            for (int i = All.Count - 1; i >= 0; i--)
            {
                var e = All[i];
                if (e._dieTime >= 0f || e._lastHitId == hitId) continue;
                float ey = e.transform.position.y, ex = e.transform.position.x;
                float eTop = ey + (e._flying ? 0.9f : 1.0f);
                if (ex < maxX && ex > minX && eTop > minY && ey < maxY)
                {
                    e._lastHitId = hitId;
                    e._hp -= damage;
                    e._hurtTime = GameConfig.EnemyHurtTime;
                    e._hitLock = GameConfig.EnemyHitLock;
                    if (e._hp <= 0) e.Die();
                    else { e._state = HIT; e._stateTime = 0.2f; e.SetX(ex + Mathf.Sign(ex - (minX + maxX) / 2f) * 0.25f); }
                    any = true;
                }
            }
            return any;
        }

        private int _lastHitId = -1;

        internal static void CheckContactAll(Vector3 ppos, float halfWidth, float height, float dt, PlayerController player)
        {
            foreach (var e in All)
            {
                if (e._dieTime >= 0f) continue;
                float ex = e.transform.position.x, ey = e.transform.position.y;
                float exHalf = 0.55f, eTop = ey + (e._flying ? 0.9f : 1.0f);
                bool overlap = ppos.x + halfWidth > ex - exHalf && ppos.x - halfWidth < ex + exHalf
                            && ppos.y + height > ey && ppos.y < eTop;
                if (overlap)
                {
                    int dmg = e._state == ATTACK && e._arch.committed ? e._arch.contactDamage : 1;
                    player.TakeDamage(dmg, ex);
                }
            }
        }

        private void Die()
        {
            _dieTime = 0.35f;
            if (GameManager.Instance != null) GameManager.Instance.OnEnemyKilled();
        }

        private void SpawnDeathBurst()
        {
            var psGo = new GameObject("DeathBurst");
            psGo.transform.position = transform.position + Vector3.up * 0.6f;
            var ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.4f; main.startLifetime = 0.45f; main.startSpeed = 3f;
            main.startSize = 0.12f; main.startColor = _arch.accent; main.playOnAwake = true;
            var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
            ps.Play();
            Destroy(psGo, 1.2f);
        }
    }
}
