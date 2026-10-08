using UnityEngine;

namespace LostRealms {
  /// Extra audio layer: music ducking on hits, procedural whoosh, chapter sting.
  [DefaultExecutionOrder(200)]
  public sealed class GrokAudioPlus : MonoBehaviour {
    AudioSource whoosh;
    float duckUntil;

    void Awake() {
      whoosh = gameObject.AddComponent<AudioSource>();
      whoosh.playOnAwake = false;
      whoosh.spatialBlend = 0;
      whoosh.clip = MakeNoise(0.12f);
    }

    public void OnLevelLoaded(RealmGame g) {
      if (!g.Save.sound) return;
      PlayWhoosh(0.28f, 0.85f + g.Realm * 0.06f);
    }

    public void OnScreen(GameScreen screen) {
      if (screen == GameScreen.Complete || screen == GameScreen.Defeated) PlayWhoosh(0.45f, 0.7f);
    }

    public void Duck(float seconds) {
      duckUntil = Time.unscaledTime + seconds;
    }

    void LateUpdate() {
      var g = RealmGame.I;
      if (!g || !g.Music) return;
      if (Time.unscaledTime >= duckUntil) return;
      float k = 0.62f;
      g.Music.volume *= k;
      var sources = g.GetComponents<AudioSource>();
      for (int i = 0; i < sources.Length; i++) {
        var s = sources[i];
        if (s && s != g.Music && s.loop && s.isPlaying) s.volume *= k;
      }
    }

    void PlayWhoosh(float vol, float pitch) {
      if (!whoosh || !whoosh.clip) return;
      var g = RealmGame.I;
      if (g && !g.Save.sound) return;
      whoosh.pitch = pitch;
      whoosh.volume = vol;
      whoosh.Play();
    }

    static AudioClip MakeNoise(float seconds) {
      int hz = 22050;
      int n = Mathf.CeilToInt(hz * seconds);
      var data = new float[n];
      float phase = 0;
      for (int i = 0; i < n; i++) {
        float t = i / (float)n;
        float env = t < 0.15f ? t / 0.15f : 1f - (t - 0.15f) / 0.85f;
        phase += (180f + 420f * (1f - t)) / hz;
        data[i] = Mathf.Sin(phase * 6.28318f) * env * 0.22f;
      }
      var clip = AudioClip.Create("GrokWhoosh", n, 1, hz, false);
      clip.SetData(data, 0);
      return clip;
    }
  }
}
