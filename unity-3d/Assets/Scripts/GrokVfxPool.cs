using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LostRealms {
  /// Zero-GC-in-steady-state spark / slash / dust bursts for mobile.
  public static class GrokVfxPool {
    const int SparkBudget = 48;
    static readonly Queue<Puff> idle = new Queue<Puff>();
    static readonly List<Puff> live = new List<Puff>();
    static Mesh quad;
    static Material mat;
    static bool ready;

    struct Puff {
      public Transform t;
      public MeshRenderer r;
      public MaterialPropertyBlock block;
      public Vector3 vel;
      public float age, life, grow;
      public Color color;
    }

    static void Ensure() {
      if (ready) return;
      quad = new Mesh { name = "Grok puff" };
      quad.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
      quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
      quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
      quad.RecalculateBounds();
      mat = new Material(Shader.Find("Sprites/Default")) { name = "Grok pooled puff" };
      for (int i = 0; i < SparkBudget; i++) idle.Enqueue(Make());
      var runner = new GameObject("Grok VFX Pool");
      Object.DontDestroyOnLoad(runner);
      runner.AddComponent<GrokVfxDriver>();
      ready = true;
    }

    static Puff Make() {
      var go = new GameObject("puff");
      Object.DontDestroyOnLoad(go);
      go.SetActive(false);
      go.AddComponent<MeshFilter>().sharedMesh = quad;
      var r = go.AddComponent<MeshRenderer>();
      r.sharedMaterial = mat;
      r.shadowCastingMode = ShadowCastingMode.Off;
      r.receiveShadows = false;
      return new Puff { t = go.transform, r = r, block = new MaterialPropertyBlock() };
    }

    public static void Sparks(Vector3 pos, Color color, int count) {
      Burst(pos, color, count, 1.6f, 2.4f, 0.45f);
    }

    public static void Dust(Vector3 pos, Color color, int count) {
      Burst(pos, color * 0.85f, count, 0.7f, 1.2f, 0.7f);
    }

    public static void Slash(Vector3 pos, Quaternion rot, Color color) {
      Burst(pos, color, 14, 2.8f, 3.4f, 0.28f);
      Vfx.Play(Vfx.Slash(RealmGame.I ? RealmGame.I.Player.Power : 0), pos, rot, 0.85f);
    }

    static void Burst(Vector3 pos, Color color, int count, float speed, float grow, float life) {
      Ensure();
      int n = Mathf.Min(count, idle.Count);
      for (int i = 0; i < n; i++) {
        var p = idle.Dequeue();
        p.t.position = pos + Random.insideUnitSphere * 0.12f;
        p.t.localScale = Vector3.one * Random.Range(0.12f, 0.28f);
        p.vel = Random.insideUnitSphere * speed + Vector3.up * (speed * 0.35f);
        p.age = 0;
        p.life = life * Random.Range(0.75f, 1.15f);
        p.grow = grow;
        p.color = color;
        p.t.gameObject.SetActive(true);
        live.Add(p);
      }
    }

    public static void Step(float dt) {
      if (!ready) return;
      var cam = Camera.main;
      for (int i = live.Count - 1; i >= 0; i--) {
        var p = live[i];
        p.age += dt;
        if (p.age >= p.life) {
          p.t.gameObject.SetActive(false);
          live.RemoveAt(i);
          idle.Enqueue(p);
          continue;
        }
        p.t.position += p.vel * dt;
        p.vel *= 1f - dt * 2.4f;
        p.t.localScale += Vector3.one * p.grow * dt * 0.08f;
        if (cam) p.t.rotation = cam.transform.rotation;
        float a = 1f - p.age / p.life;
        var c = p.color; c.a = a * 0.9f;
        p.block.SetColor("_Color", c);
        p.r.SetPropertyBlock(p.block);
        live[i] = p;
      }
    }
  }

  sealed class GrokVfxDriver : MonoBehaviour {
    void LateUpdate() { GrokVfxPool.Step(Time.unscaledDeltaTime); }
  }
}
