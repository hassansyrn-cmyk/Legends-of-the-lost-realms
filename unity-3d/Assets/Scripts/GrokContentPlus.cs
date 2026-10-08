using UnityEngine;

namespace LostRealms {
  /// Extra chapter atmosphere: floating motes and a few trail lanterns.
  /// Island collision stays owned by RealmWorld.
  public sealed class GrokContentPlus : MonoBehaviour {
    Transform motes;
    int builtFor = -1;
    Material lampMat;

    public bool NeedsRefresh(RealmGame g) {
      return g.World && g.World.Route != null && g.World.Route.Count > 0 && builtFor != g.Level;
    }

    public void OnLevelLoaded(RealmGame g) {
      if (motes) Destroy(motes.gameObject);
      motes = null;
      if (!g.World || g.World.Route == null || g.World.Route.Count == 0) return;
      builtFor = g.Level;
      Color accent = RealmAccent(g);
      motes = new GameObject("Grok atmosphere motes").transform;
      motes.SetParent(g.World.transform, false);
      motes.position = g.World.Spawn + Vector3.up * 4f;
      var go = motes.gameObject;
      var ps = go.AddComponent<ParticleSystem>();
      var main = ps.main;
      main.startLifetime = 6f;
      main.startSpeed = 0.35f;
      main.startSize = 0.08f;
      int cap = QualityCap();
      main.maxParticles = cap;
      main.startColor = new Color(accent.r, accent.g, accent.b, 0.55f);
      main.simulationSpace = ParticleSystemSimulationSpace.World;
      var emission = ps.emission;
      emission.rateOverTime = cap > 40 ? 14f : 6f;
      var shape = ps.shape;
      shape.shapeType = ParticleSystemShapeType.Box;
      shape.scale = new Vector3(28f, 10f, 80f);
      var renderer = go.GetComponent<ParticleSystemRenderer>();
      renderer.material = new Material(Shader.Find("Sprites/Default"));
      PlaceLanterns(g, accent);
    }

    static Color RealmAccent(RealmGame g) {
      int i = Mathf.Clamp(g.Realm, 0, RealmGame.Accents.Length - 1);
      return RealmGame.Accents[i];
    }

    static int QualityCap() {
      var q = Object.FindFirstObjectByType<GrokMobileQuality>();
      if (!q) return 48;
      return q.Current == GrokMobileQuality.Tier.Low ? 18 : q.Current == GrokMobileQuality.Tier.Medium ? 36 : 64;
    }

    void PlaceLanterns(RealmGame g, Color accent) {
      var q = Object.FindFirstObjectByType<GrokMobileQuality>();
      bool lights = !q || q.Current != GrokMobileQuality.Tier.Low;
      if (!lampMat) {
        lampMat = new Material(Shader.Find("Standard")) { name = "Grok lantern shared" };
        lampMat.EnableKeyword("_EMISSION");
      }
      lampMat.color = accent;
      lampMat.SetColor("_EmissionColor", accent * 1.6f);
      int placed = 0;
      int n = 0;
      foreach (var node in g.World.Route) {
        n++;
        if ((n % 3) != 0) continue;
        if (placed >= 4) break;
        var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lamp.name = "Grok trail lantern";
        lamp.transform.SetParent(motes, true);
        lamp.transform.position = node + Vector3.up * 1.7f;
        lamp.transform.localScale = Vector3.one * 0.22f;
        var col = lamp.GetComponent<Collider>();
        if (col) Destroy(col);
        var r = lamp.GetComponent<Renderer>();
        r.sharedMaterial = lampMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (lights && placed < 2) {
          var light = lamp.AddComponent<Light>();
          light.type = LightType.Point;
          light.range = 3.6f;
          light.intensity = 0.85f;
          light.color = accent;
          light.shadows = LightShadows.None;
        }
        placed++;
      }
    }
  }
}
