using UnityEngine;
using UnityEngine.Rendering;

namespace LostRealms {
    /// <summary>
    /// Lightweight, mobile-optimized snowfall system for the frozen realm.
    /// Follows the camera/player XZ position while maintaining a fixed height above.
    /// Uses world-space particle simulation so existing snowflakes fall naturally without tracking camera movement.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    [AddComponentMenu("Lost Realms/Snow Follow Camera")]
    public class SnowFollowCamera : MonoBehaviour {
        [Header("Target & Positioning")]
        [Tooltip("The camera or player transform to follow. If unassigned, automatically finds MainCamera or Player.")]
        [SerializeField] private Transform targetTransform;
        [Tooltip("Vertical height offset above the target to position the snow emission box.")]
        [SerializeField] private float heightOffset = 8.0f;
        [Tooltip("Size of the emission box covering the gameplay view area.")]
        [SerializeField] private Vector3 spawnAreaSize = new Vector3(22f, 1f, 16f);

        [Header("Snow Particle Settings")]
        [Tooltip("Rate of snowflakes emitted per second.")]
        [Range(10f, 300f)] [SerializeField] private float snowIntensity = 100f;
        [Tooltip("Maximum allowed simultaneous particles for mobile performance (recommended: 500-800).")]
        [Range(100, 1200)] [SerializeField] private int maxParticles = 800;
        [Tooltip("Downward speed factor for the snowfall.")]
        [Range(0.5f, 5.0f)] [SerializeField] private float snowfallSpeed = 2.0f;

        [Header("Wind Simulation")]
        [Tooltip("Direction vector of the wind.")]
        [SerializeField] private Vector3 windDirection = new Vector3(1f, 0f, 0.2f);
        [Tooltip("Strength of the horizontal wind velocity.")]
        [Range(0f, 5.0f)] [SerializeField] private float windStrength = 1.2f;

        [Header("Mobile & Realm Optimization")]
        [Tooltip("When enabled, pauses/stops emission when outside the snow realm (Realm 2).")]
        [SerializeField] private bool onlyInSnowRealm = true;
        [Tooltip("Realm index for the snowy/frozen realm.")]
        [SerializeField] private int snowRealmIndex = 2;
        [Tooltip("Automatically configure particle system modules on Awake or Inspector Reset.")]
        [SerializeField] private bool autoConfigureParticleSystem = true;
        [Tooltip("Optional ground mist layer for added cold atmosphere.")]
        [SerializeField] private bool createGroundMist = false;

        private ParticleSystem ps;
        private ParticleSystemRenderer psRenderer;
        private Material snowMaterial;
        private bool isConfigured;

        public Transform Target {
            get => targetTransform;
            set => targetTransform = value;
        }

        public float HeightOffset {
            get => heightOffset;
            set => heightOffset = value;
        }

        public float SnowIntensity {
            get => snowIntensity;
            set {
                snowIntensity = value;
                UpdateEmission();
            }
        }

        public float WindStrength {
            get => windStrength;
            set {
                windStrength = value;
                UpdateVelocity();
            }
        }

        public Vector3 WindDirection {
            get => windDirection;
            set {
                windDirection = value;
                UpdateVelocity();
            }
        }

        public Vector3 SpawnAreaSize {
            get => spawnAreaSize;
            set {
                spawnAreaSize = value;
                UpdateShape();
            }
        }

        public int MaxParticles {
            get => maxParticles;
            set {
                maxParticles = value;
                UpdateMain();
            }
        }

        public float SnowfallSpeed {
            get => snowfallSpeed;
            set {
                snowfallSpeed = value;
                UpdateMain();
                UpdateVelocity();
            }
        }

        private void Awake() {
            ps = GetComponent<ParticleSystem>();
            psRenderer = GetComponent<ParticleSystemRenderer>();
            if (autoConfigureParticleSystem) {
                ConfigureParticleSystem();
            }
            if (createGroundMist) {
                CreateGroundMistObject();
            }
        }

        private void Reset() {
            ps = GetComponent<ParticleSystem>();
            psRenderer = GetComponent<ParticleSystemRenderer>();
            ConfigureParticleSystem();
        }

        private void OnValidate() {
            if (ps != null && isConfigured) {
                UpdateEmission();
                UpdateShape();
                UpdateVelocity();
                UpdateMain();
            }
        }

        /// <summary>
        /// Applies the exact optimized particle system configuration specified for mobile snow.
        /// </summary>
        [ContextMenu("Configure Particle System")]
        public void ConfigureParticleSystem() {
            if (ps == null) ps = GetComponent<ParticleSystem>();
            if (psRenderer == null) psRenderer = GetComponent<ParticleSystemRenderer>();
            if (ps == null) return;

            // Stop emission while configuring
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // 1. Main Module
            var main = ps.main;
            main.duration = 10f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.gravityModifier = 0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.playOnAwake = false;

            // 2. Emission Module
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = snowIntensity;

            // 3. Shape Module (Box emitter covering visible area)
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = spawnAreaSize;

            // 4. Velocity Over Lifetime (Downward + Wind + slight turbulence)
            // Note: All 3 axes MUST share the exact same curve mode (TwoConstants) on mobile!
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            Vector3 normWind = windDirection.sqrMagnitude > 0.001f ? windDirection.normalized : Vector3.right;
            float wx = normWind.x * windStrength;
            float wz = normWind.z * windStrength;
            float downSpeed = -snowfallSpeed;
            vel.x = new ParticleSystem.MinMaxCurve(wx - 0.4f, wx + 0.4f);
            vel.y = new ParticleSystem.MinMaxCurve(downSpeed - 0.4f, downSpeed + 0.4f);
            vel.z = new ParticleSystem.MinMaxCurve(wz - 0.4f, wz + 0.4f);

            // 5. Noise Module (Low quality for Android)
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            noise.frequency = 0.2f;
            noise.scrollSpeed = 0.1f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Low;

            // 6. Rotation Over Lifetime (Slow randomized rotation)
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.x = new ParticleSystem.MinMaxCurve(0f);
            rot.y = new ParticleSystem.MinMaxCurve(0f);
            rot.z = new ParticleSystem.MinMaxCurve(-40f * Mathf.Deg2Rad, 40f * Mathf.Deg2Rad);

            // 7. Color Over Lifetime (Soft white, gradual fade out)
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.92f, 0.96f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 1f, 1f), 0.7f),
                    new GradientColorKey(new Color(0.88f, 0.94f, 1f), 1f)
                },
                new[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.85f, 0.08f),
                    new GradientAlphaKey(0.80f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(gradient);

            // 8. Renderer Module
            if (psRenderer != null) {
                psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                psRenderer.shadowCastingMode = ShadowCastingMode.Off;
                psRenderer.receiveShadows = false;

                if (snowMaterial == null) {
                    var tex = Resources.Load<Texture2D>("VFX/Textures/circle_01");
                    var shader = Shader.Find("Mobile/Particles/Alpha Blended")
                              ?? Shader.Find("Particles/Standard Unlit")
                              ?? Shader.Find("Sprites/Default");
                    snowMaterial = new Material(shader) { name = "SnowParticleMaterial" };
                    if (tex != null) snowMaterial.mainTexture = tex;
                    snowMaterial.color = new Color(1f, 1f, 1f, 0.85f);
                    snowMaterial.enableInstancing = true;
                    snowMaterial.renderQueue = 3000;
                    // Fix black squares on mobile: Particles/Standard Unlit
                    // defaults to opaque when the Mobile shader is stripped from
                    // the build.  Force alpha-blend state so the snowflake
                    // texture's transparent background is honoured on every device.
                    snowMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    snowMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    snowMaterial.SetInt("_ZWrite", 0);
                    snowMaterial.DisableKeyword("_ALPHATEST_ON");
                    snowMaterial.EnableKeyword("_ALPHABLEND_ON");
                    snowMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    if (snowMaterial.HasProperty("_Mode")) snowMaterial.SetFloat("_Mode", 2f);
                }
                psRenderer.sharedMaterial = snowMaterial;
            }

            isConfigured = true;
            ps.Play();
        }

        private void UpdateEmission() {
            if (ps != null) {
                var emission = ps.emission;
                emission.rateOverTime = snowIntensity;
            }
        }

        private void UpdateShape() {
            if (ps != null) {
                var shape = ps.shape;
                shape.scale = spawnAreaSize;
            }
        }

        private void UpdateMain() {
            if (ps != null) {
                var main = ps.main;
                main.maxParticles = maxParticles;
            }
        }

        private void UpdateVelocity() {
            if (ps != null) {
                var vel = ps.velocityOverLifetime;
                Vector3 normWind = windDirection.sqrMagnitude > 0.001f ? windDirection.normalized : Vector3.right;
                float wx = normWind.x * windStrength;
                float wz = normWind.z * windStrength;
                float downSpeed = -snowfallSpeed;
                vel.x = new ParticleSystem.MinMaxCurve(wx - 0.4f, wx + 0.4f);
                vel.y = new ParticleSystem.MinMaxCurve(downSpeed - 0.4f, downSpeed + 0.4f);
                vel.z = new ParticleSystem.MinMaxCurve(wz - 0.4f, wz + 0.4f);
            }
        }

        private void LateUpdate() {
            // Find target if not explicitly assigned
            if (targetTransform == null) {
                if (Camera.main != null) targetTransform = Camera.main.transform;
                else if (RealmGame.I != null && RealmGame.I.Player != null) targetTransform = RealmGame.I.Player.transform;
            }

            if (targetTransform == null) return;

            // Check if active level is the snowy realm
            if (onlyInSnowRealm && RealmGame.I != null) {
                bool isSnowRealm = (RealmGame.I.Realm == snowRealmIndex) && (RealmGame.I.Screen == GameScreen.Playing);
                if (!isSnowRealm) {
                    if (ps != null && ps.isPlaying) ps.Pause();
                    return;
                } else {
                    if (ps != null && ps.isPaused) ps.Play();
                }
            }

            // Follow camera/target X and Z position while maintaining fixed height
            Vector3 targetPos = targetTransform.position;
            transform.position = new Vector3(targetPos.x, targetPos.y + heightOffset, targetPos.z);
        }

        private void CreateGroundMistObject() {
            var mistGo = new GameObject("GroundMist");
            mistGo.transform.SetParent(transform, false);
            mistGo.transform.localPosition = new Vector3(0, -heightOffset + 1.2f, 0);

            var mistPs = mistGo.AddComponent<ParticleSystem>();
            var mistMain = mistPs.main;
            mistMain.duration = 15f;
            mistMain.loop = true;
            mistMain.startLifetime = 8f;
            mistMain.startSpeed = 0.3f;
            mistMain.startSize = new ParticleSystem.MinMaxCurve(4f, 8f);
            mistMain.startColor = new Color(0.85f, 0.92f, 1f, 0.12f);
            mistMain.maxParticles = 20;
            mistMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var mistEm = mistPs.emission;
            mistEm.rateOverTime = 2.5f;

            var mistShape = mistPs.shape;
            mistShape.shapeType = ParticleSystemShapeType.Box;
            mistShape.scale = new Vector3(25f, 1f, 25f);

            var mistRen = mistGo.GetComponent<ParticleSystemRenderer>();
            var mistTex = Resources.Load<Texture2D>("VFX/Textures/smoke_04");
            var mistMat = new Material(Shader.Find("Sprites/Default")) { name = "GroundMistMaterial" };
            if (mistTex) mistMat.mainTexture = mistTex;
            mistRen.sharedMaterial = mistMat;
            mistRen.shadowCastingMode = ShadowCastingMode.Off;
            mistRen.receiveShadows = false;
        }

        private void OnDestroy() {
            if (snowMaterial != null) {
                Destroy(snowMaterial);
            }
        }

        /// <summary>
        /// Factory method to instantiate and configure the SnowSystem GameObject programmatically.
        /// </summary>
        public static GameObject Create(Transform parent = null, Transform target = null) {
            var go = new GameObject("SnowSystem");
            if (parent != null) go.transform.SetParent(parent, false);

            var snow = go.AddComponent<SnowFollowCamera>();
            if (target != null) snow.Target = target;
            snow.ConfigureParticleSystem();
            return go;
        }
    }
}
