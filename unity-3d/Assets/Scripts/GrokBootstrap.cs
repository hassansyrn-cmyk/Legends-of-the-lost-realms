using UnityEngine;

namespace LostRealms {
  /// Grok upgrade director. Additive — does not change Aster clips or locked
  /// locomotion constants (coyote 0.14, buffer 0.13, jump 8.4, move 4.8).
  [DefaultExecutionOrder(-40)]
  public sealed class GrokBootstrap : MonoBehaviour {
    static GrokBootstrap instance;
    GrokFeel feel;
    GrokCombatPlus combat;
    GrokContentPlus content;
    GrokGameplayPlus gameplay;
    GrokMobileQuality quality;
    GrokAudioPlus audioPlus;
    int lastLevel = -1;
    GameScreen lastScreen;
    bool qualityLocked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
      if (instance) return;
      var go = new GameObject("Grok Upgrade Director");
      DontDestroyOnLoad(go);
      instance = go.AddComponent<GrokBootstrap>();
    }

    void Awake() {
      quality = gameObject.AddComponent<GrokMobileQuality>();
      feel = gameObject.AddComponent<GrokFeel>();
      combat = gameObject.AddComponent<GrokCombatPlus>();
      content = gameObject.AddComponent<GrokContentPlus>();
      gameplay = gameObject.AddComponent<GrokGameplayPlus>();
      audioPlus = gameObject.AddComponent<GrokAudioPlus>();
      gameObject.AddComponent<GrokPhysicsPlus>();
      quality.Apply();
    }

    void Update() {
      var g = RealmGame.I;
      if (!g) return;
      if (!qualityLocked) {
        quality.Apply();
        qualityLocked = true;
      }
      if (g.Level != lastLevel || g.World && content.NeedsRefresh(g)) {
        lastLevel = g.Level;
        content.OnLevelLoaded(g);
        combat.OnLevelLoaded(g);
        gameplay.OnLevelLoaded(g);
        audioPlus.OnLevelLoaded(g);
        feel.OnLevelLoaded(g);
      }
      if (g.Screen != lastScreen) {
        lastScreen = g.Screen;
        audioPlus.OnScreen(g.Screen);
      }
      feel.Tick(g);
      combat.Tick(g);
      gameplay.Tick(g);
    }
  }
}
