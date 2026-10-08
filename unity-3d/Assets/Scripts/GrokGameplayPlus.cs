using System.Collections.Generic;
using UnityEngine;

namespace LostRealms {
  /// Optional aether wisps along the route. Walking into one restores a little
  /// energy once. Does not change damage, jump, or move constants.
  public sealed class GrokGameplayPlus : MonoBehaviour {
    readonly List<Wisp> wisps = new List<Wisp>();
    Material mat;

    struct Wisp {
      public Transform t;
      public bool taken;
    }

    public void OnLevelLoaded(RealmGame g) {
      for (int i = 0; i < wisps.Count; i++) if (wisps[i].t) Destroy(wisps[i].t.gameObject);
      wisps.Clear();
      if (!g.World || g.World.Route == null || g.Screen == GameScreen.Menu) return;
      if (!mat) {
        mat = new Material(Shader.Find("Sprites/Default")) { name = "Grok aether wisp" };
        mat.color = new Color(0.45f, 0.95f, 1f, 0.9f);
      }
      int n = 0;
      foreach (var node in g.World.Route) {
        n++;
        if (n < 3 || (n % 4) != 0) continue;
        if (wisps.Count >= 3) break;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Grok aether wisp";
        if (g.World) go.transform.SetParent(g.World.transform, true);
        go.transform.position = node + Vector3.up * 1.15f;
        go.transform.localScale = Vector3.one * 0.38f;
        var col = go.GetComponent<Collider>();
        if (col) Destroy(col);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        wisps.Add(new Wisp { t = go.transform });
      }
    }

    public void Tick(RealmGame g) {
      if (g.Screen != GameScreen.Playing || !g.Player) return;
      Vector3 p = g.Player.transform.position;
      for (int i = 0; i < wisps.Count; i++) {
        var w = wisps[i];
        if (w.taken || !w.t) continue;
        w.t.Rotate(0f, 90f * Time.unscaledDeltaTime, 0f, Space.World);
        w.t.position += Vector3.up * Mathf.Sin(Time.unscaledTime * 2.2f + i) * 0.15f * Time.unscaledDeltaTime;
        if ((w.t.position - p).sqrMagnitude < 1.35f * 1.35f) {
          w.taken = true;
          g.Player.Energy = Mathf.Min(100f, g.Player.Energy + 14f);
          GrokVfxPool.Sparks(w.t.position, new Color(0.4f, 1f, 0.95f), 14);
          if (g.Save.sound) g.Sound("gem");
          g.Tell("AETHER RESTORED", 1.1f);
          Destroy(w.t.gameObject);
        }
        wisps[i] = w;
      }
    }
  }
}
