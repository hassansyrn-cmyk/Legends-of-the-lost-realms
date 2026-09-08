using System.Collections.Generic;
using UnityEngine;

namespace LostRealms
{
    /// Spinning coin (sun-mote) or gem. Collect on proximity to the hero.
    public class Pickup : MonoBehaviour
    {
        private static readonly List<Pickup> All = new List<Pickup>();
        private bool _gem;
        private float _seed;
        private bool _collected;

        public static Pickup Create(Vector2 pos, bool gem)
        {
            var go = GameObject.CreatePrimitive(gem ? PrimitiveType.Sphere : PrimitiveType.Cylinder);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = gem ? "Gem" : "Coin";
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.GetComponent<MeshRenderer>().sharedMaterial = gem ? LevelBuilder.GemMat() : LevelBuilder.CoinMat();
            if (!gem) go.transform.localScale = new Vector3(0.22f, 0.04f, 0.22f);
            else go.transform.localScale = Vector3.one * 0.3f;
            var p = go.AddComponent<Pickup>();
            p._gem = gem;
            p._seed = Random.value * 7f;
            All.Add(p);
            return p;
        }

        private void OnDestroy() { All.Remove(this); }

        private void Update()
        {
            transform.Rotate(Vector3.up * (120f + _seed * 6f) * Time.deltaTime, Space.World);
            var p = transform.position;
            p.y += Mathf.Sin(Time.time * 2.6f + _seed) * Time.deltaTime * 0.3f;
            transform.position = p;
        }

        internal static void CheckCollectAll(Vector3 heroPos)
        {
            for (int i = All.Count - 1; i >= 0; i--)
            {
                var p = All[i];
                if (p._collected) continue;
                Vector2 d = p.transform.position - heroPos;
                if (d.sqrMagnitude < 1.1f * 1.1f)
                {
                    p._collected = true;
                    if (GameManager.Instance != null) GameManager.Instance.OnPickup(p._gem);
                    Object.Destroy(p.gameObject);
                }
            }
        }
    }
}
