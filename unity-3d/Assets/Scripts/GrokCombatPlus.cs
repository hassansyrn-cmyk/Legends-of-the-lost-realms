using System.Collections.Generic;
using UnityEngine;

namespace LostRealms {
  /// Extra combat readability: elite auras, execution tells, parry spark.
  /// Does not alter damage math in Hero.Attack.
  public sealed class GrokCombatPlus : MonoBehaviour {
    readonly Dictionary<int, float> lastHp = new Dictionary<int, float>();
    readonly Dictionary<int, Transform> auras = new Dictionary<int, Transform>();
    readonly List<int> drop = new List<int>();
    bool wasCounter;

    public void OnLevelLoaded(RealmGame g) {
      lastHp.Clear();
      foreach (var kv in auras) if (kv.Value) Object.Destroy(kv.Value.gameObject);
      auras.Clear();
    }

    public void Tick(RealmGame g) {
      if (g.Screen != GameScreen.Playing || !g.Player) return;
      var hero = g.Player;
      bool counter = hero.CounterReady;
      if (counter && !wasCounter) {
        GrokVfxPool.Sparks(hero.transform.position + Vector3.up * 1.4f, new Color(0.35f, 1f, 0.95f), 16);
        g.Tell("COUNTER WINDOW", 0.9f);
        var audio = GetComponent<GrokAudioPlus>();
        if (audio) audio.Duck(0.18f);
      }
      wasCounter = counter;

      drop.Clear();
      for (int i = 0; i < g.Enemies.Count; i++) {
        var e = g.Enemies[i];
        if (!e) continue;
        int id = e.GetInstanceID();
        if (e.Health <= 0) { drop.Add(id); continue; }
        if (!lastHp.TryGetValue(id, out float prev)) prev = e.Health;
        if (e.Health < prev - 0.05f) {
          Color c = e.Boss ? new Color(1f, 0.55f, 0.2f) : RealmGame.ElementColors[Mathf.Clamp(g.Player.Power, 0, 2)];
          GrokVfxPool.Sparks(e.transform.position + Vector3.up * (e.Boss ? 1.6f : 0.9f), c, e.Boss ? 18 : 8);
          if (e.Health / Mathf.Max(1f, e.MaxHealth) < 0.22f && !e.Boss)
            GrokVfxPool.Sparks(e.transform.position + Vector3.up * 1.2f, new Color(1f, 0.85f, 0.4f), 12);
          var feel = GetComponent<GrokFeel>();
          if (feel && e.Boss) feel.AddTrauma(0.16f);
        }
        lastHp[id] = e.Health;
        if (e.Boss || e.Kind == 6 || e.Kind == 7) EnsureAura(e, id);
      }
      for (int i = 0; i < drop.Count; i++) {
        int id = drop[i];
        if (auras.TryGetValue(id, out var aura) && aura) Object.Destroy(aura.gameObject);
        auras.Remove(id);
        lastHp.Remove(id);
      }
    }

    void EnsureAura(Enemy e, int id) {
      if (auras.TryGetValue(id, out var existing) && existing) return;
      Color tint = e.Boss ? new Color(1f, 0.45f, 0.2f, 0.45f) : new Color(0.7f, 0.85f, 1f, 0.35f);
      var ring = CombatTelegraph.Ring(e.transform, e.Boss ? 2.4f : 1.35f, tint, "Grok elite aura");
      ring.transform.localPosition = Vector3.up * 0.04f;
      auras[id] = ring.transform;
    }
  }
}
