using System.IO;
using UnityEngine;

namespace LostRealms
{
    /// Loads level JSON from StreamingAssets and constructs the whole runtime world:
    /// platforms, pickups, foes, hazards, hero, guide light. Collision data goes to LevelRegistry.
    public class LevelBuilder : MonoBehaviour
    {
        [Tooltip("Aster mesh prefab (FBX). Replaced by the Mixamo rigged prefab once animations land.")]
        public GameObject heroModelPrefab;
        public int levelId = 1;

        private void Awake()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "levels", $"level_{levelId}.json");
#if UNITY_ANDROID && !UNITY_EDITOR
            // StreamingAssets is inside the APK on Android; read via UnityWebRequest synchronously.
            var request = UnityEngine.Networking.UnityWebRequest.Get(path);
            request.SendWebRequest();
            while (!request.isDone) { }
            BuildFromJson(request.downloadHandler.text);
#else
            BuildFromJson(File.ReadAllText(path));
#endif
        }

        private void BuildFromJson(string json)
        {
            LevelSchema level = JsonUtility.FromJson<LevelSchema>(json);
            if (level == null || level.platforms == null)
            {
                Debug.LogError($"LevelBuilder: failed to parse level {levelId}");
                return;
            }

            LevelRegistry.Clear();
            LevelRegistry.Checkpoint = new Vector2(GameConfig.WorldX(level.checkpoint.x), GameConfig.WorldY(level.checkpoint.y));

            foreach (var p in level.platforms)
            {
                float left = GameConfig.WorldX(p.x);
                float top = GameConfig.WorldY(p.y);
                float right = GameConfig.WorldX(p.x + p.width);
                bool groundRow = Mathf.Abs(p.y - 620f) < 5f;   // main walkway row in level 1

                BuildPlatformMesh(left, right, top, groundRow);
                LevelRegistry.Platforms.Add(new LevelRegistry.PlatformCol { Left = left, Right = right, Top = top, IsGroundRow = groundRow });
            }

            foreach (var h in level.hazards)
            {
                float minX = GameConfig.WorldX(h.left), maxX = GameConfig.WorldX(h.right);
                float wMinY = GameConfig.WorldY(h.bottom), wMaxY = GameConfig.WorldY(h.top);
                BuildHazardMesh(minX, maxX, wMinY, wMaxY);
                LevelRegistry.Hazards.Add(new LevelRegistry.HazardCol { MinX = minX, MaxX = maxX, MinY = wMinY, MaxY = wMaxY });
            }

            foreach (var pk in level.pickups)
                Pickup.Create(new Vector2(GameConfig.WorldX(pk.x), GameConfig.WorldY(pk.y)), pk.gem);

            foreach (var f in level.foes)
                Enemy.Create(new Vector2(GameConfig.WorldX(f.x), GameConfig.WorldY(f.y)), f.kind);

            BuildHero();
            BuildMosslight();
            LevelRegistry.Loaded = true;

            if (GameManager.Instance != null) GameManager.Instance.OnLevelLoaded(level);
            Debug.Log($"LevelBuilder: built '{level.stageName}' — {level.platforms.Count} platforms, {level.foes.Count} foes, {level.pickups.Count} pickups");
        }

        // ---------------------------------------------------------------- geometry

        private static Material _stoneMat, _mossMat, _spikeMat, _mosslightMat, _coinMat, _gemMat;

        private static Material StoneMat()
        {
            if (_stoneMat == null) _stoneMat = MakeMat(new Color32(0x4A, 0x55, 0x68, 255));
            return _stoneMat;
        }
        private static Material MossMat()
        {
            if (_mossMat == null) _mossMat = MakeMat(new Color32(0x5C, 0x8A, 0x5A, 255));
            return _mossMat;
        }
        private static Material SpikeMat()
        {
            if (_spikeMat == null) _spikeMat = MakeMat(new Color32(0x8A, 0x5A, 0x4A, 255));
            return _spikeMat;
        }
        private static Material MosslightMat()
        {
            if (_mosslightMat == null) _mosslightMat = MakeMat(new Color32(0xBD, 0xF5, 0xA0, 255), emissive: true);
            return _mosslightMat;
        }
        public static Material CoinMat()
        {
            if (_coinMat == null) _coinMat = MakeMat(new Color32(0xF2, 0xC9, 0x4E, 255), emissive: true);
            return _coinMat;
        }
        public static Material GemMat()
        {
            if (_gemMat == null) _gemMat = MakeMat(new Color32(0xB5, 0x8C, 0xFF, 255), emissive: true);
            return _gemMat;
        }

        private static Material MakeMat(Color c, bool emissive = false)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (m == null || m.shader == null || !m.shader.isSupported)
                m = new Material(Shader.Find("Standard"));
            m.color = c;
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 1.4f);
            }
            return m;
        }

        private static GameObject QuadBox(Vector3 center, Vector3 size, Material mat, string name, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.Destroy(go.GetComponent<BoxCollider>());   // collision is hand-rolled
            return go;
        }

        private void BuildPlatformMesh(float left, float right, float top, bool groundRow)
        {
            var root = new GameObject($"Platform_{left:F1}");
            root.transform.SetParent(transform, false);
            float width = right - left;
            float thickness = groundRow ? 1.2f : 0.45f;
            QuadBox(new Vector3(width / 2f, -thickness / 2f, 0f), new Vector3(width, thickness, 1.1f), StoneMat(), "slab", root.transform);
            QuadBox(new Vector3(width / 2f, 0.012f, 0f), new Vector3(width, 0.06f, 1.14f), MossMat(), "moss_top", root.transform);
            root.transform.position = new Vector3(left, top, 0f);
        }

        private void BuildHazardMesh(float minX, float maxX, float minY, float maxY)
        {
            var root = new GameObject("RootSpikes");
            root.transform.SetParent(transform, false);
            float width = maxX - minX, height = maxY - minY;
            QuadBox(new Vector3(width / 2f, height / 2f, 0f), new Vector3(width, height, 0.9f), SpikeMat(), "spikes", root.transform);
            int count = Mathf.Max(2, (int)(width / 0.22f));
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                var thorn = QuadBox(new Vector3(t * width - width / 2f, height + 0.07f, 0f), new Vector3(0.06f, 0.16f, 0.7f), SpikeMat(), "thorn", root.transform);
                thorn.transform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0) ? 8f : -8f);
            }
            root.transform.position = new Vector3(minX, minY, 0f);
        }

        private void BuildMosslight()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Mosslight";
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = MosslightMat();
            go.transform.localScale = Vector3.one * 0.28f;
            go.transform.position = new Vector3(LevelRegistry.Checkpoint.x + 1.5f, LevelRegistry.Checkpoint.y + 2.4f, -0.5f);
            go.AddComponent<Mosslight>();
        }

        // ---------------------------------------------------------------- hero

        private void BuildHero()
        {
            var heroGo = new GameObject("Aster");
            heroGo.transform.position = new Vector3(LevelRegistry.Checkpoint.x, LevelRegistry.Checkpoint.y, 0f);
            var player = heroGo.AddComponent<PlayerController>();

            if (heroModelPrefab != null)
            {
                var model = Instantiate(heroModelPrefab, heroGo.transform);
                model.name = "Model";
                // Aster's FBX faces -Z; the camera looks along +Z, so turn the model to face +X
                // and mirror via scale.x for direction flips.
                model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                player.visualRoot = model.transform;

                // Normalize import scale so the hero is HeroHeight tall regardless of FBX units.
                var bounds = CalculateWorldBounds(model);
                if (bounds.HasValue)
                {
                    float h = bounds.Value.size.y;
                    if (h > 0.01f)
                    {
                        float s = GameConfig.HeroHeight / h;
                        model.transform.localScale = Vector3.one * s;
                        var b2 = CalculateWorldBounds(model).Value;
                        // put the model's feet on the parent origin (world offset ≈ local, parent unscaled)
                        model.transform.localPosition += new Vector3(0f, transform.position.y - b2.min.y, 0f);
                    }
                }
            }
            else
            {
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.Destroy(capsule.GetComponent<Collider>());
                capsule.name = "Model";
                capsule.transform.SetParent(heroGo.transform, false);
                capsule.transform.localScale = new Vector3(0.9f, GameConfig.HeroHeight * 0.5f, 0.9f);
                capsule.transform.localPosition = new Vector3(0f, GameConfig.HeroHeight * 0.5f, 0f);
                capsule.GetComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color32(0x3E, 0x6C, 0xB0, 255));
                player.visualRoot = capsule.transform;
            }
        }

        private static Bounds? CalculateWorldBounds(GameObject go)
        {
            Bounds? bounds = null;
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                if (bounds == null) bounds = r.bounds;
                else
                {
                    var b = bounds.Value;
                    b.Encapsulate(r.bounds);
                    bounds = b;
                }
            }
            return bounds;
        }
    }

    /// The level guide light from the design brief — drifts ahead of the player, gentle bob.
    public class Mosslight : MonoBehaviour
    {
        private float _seed;
        private void Start() { _seed = Random.value * 10f; }
        private void Update()
        {
            var p = transform.position;
            if (PlayerController.Instance != null)
                p.x = Mathf.Lerp(p.x, PlayerController.Instance.transform.position.x + 1.6f, Time.deltaTime * 0.6f);
            p.x = Mathf.Clamp(p.x, 1f, GameConfig.LevelEndX - 0.5f);
            p.y += Mathf.Sin(Time.time * 2.1f + _seed) * Time.deltaTime * 0.55f;
            transform.position = p;
        }
    }
}
