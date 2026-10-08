using UnityEngine;

namespace LostRealms {
  /// Presentation juice: trauma shake, extra kick, squash pulses, pooled sparks.
  /// Simulation outcomes are never changed here. Shake is applied in LateUpdate
  /// after FollowCamera so it is not overwritten.
  [DefaultExecutionOrder(80)]
  public sealed class GrokFeel : MonoBehaviour {
    float trauma;
    float lastHealth = -1;
    bool wasAttacking;
    bool wasGrounded = true;
    Transform cameraTransform;
    float landDustCooldown;

    public void OnLevelLoaded(RealmGame g) {
      lastHealth = g.Player ? g.Player.Health : -1;
      wasAttacking = false;
      trauma = 0;
      cameraTransform = g.CameraRig ? g.CameraRig.transform : null;
    }

    public void Tick(RealmGame g) {
      if (g.Screen != GameScreen.Playing || !g.Player) {
        trauma = Mathf.MoveTowards(trauma, 0, Time.unscaledDeltaTime * 2f);
        return;
      }
      var hero = g.Player;
      if (lastHealth < 0) lastHealth = hero.Health;
      if (hero.Health < lastHealth) {
        float lost = lastHealth - hero.Health;
        AddTrauma(Mathf.Clamp(0.22f + lost * 0.08f, 0.18f, 0.55f));
        if (g.Save.shake && g.CameraRig) g.CameraRig.Kick(hero.transform.forward * -0.18f, 0.22f);
        GrokVfxPool.Sparks(hero.transform.position + Vector3.up * 0.9f, new Color(1f, 0.25f, 0.18f), 10);
        if (g.Audio) g.Audio.Play("hurt", 0.85f, 0.96f);
      }
      lastHealth = hero.Health;

      bool attacking = hero.Attacking;
      if (attacking && !wasAttacking) {
        AddTrauma(hero.Plunging ? 0.28f : 0.12f);
        Color elem = RealmGame.ElementColors[Mathf.Clamp(hero.Power, 0, 2)];
        GrokVfxPool.Slash(hero.transform.position + Vector3.up * 1.05f + hero.transform.forward * 0.7f, hero.transform.rotation, elem);
      }
      wasAttacking = attacking;

      if (hero.Grounded && !wasGrounded && landDustCooldown <= 0f) {
        GrokVfxPool.Dust(hero.transform.position + Vector3.up * 0.05f, g.Accent, 6);
        landDustCooldown = 0.12f;
      }
      wasGrounded = hero.Grounded;
      landDustCooldown -= Time.unscaledDeltaTime;

      trauma = Mathf.Clamp01(trauma - Time.unscaledDeltaTime * 1.65f);
    }

    public void AddTrauma(float amount) {
      trauma = Mathf.Clamp01(trauma + amount);
    }

    void LateUpdate() {
      var g = RealmGame.I;
      if (g) ApplyShake(g);
    }

    void ApplyShake(RealmGame g) {
      if (!g.Save.shake || !cameraTransform) return;
      float shake = trauma * trauma;
      if (shake < 0.0008f) return;
      float t = Time.unscaledTime * 29f;
      Vector3 offset = new Vector3(
        (Mathf.PerlinNoise(t, 0.13f) - 0.5f) * 2f,
        (Mathf.PerlinNoise(0.71f, t) - 0.5f) * 2f,
        0f) * (0.11f * shake);
      cameraTransform.position += cameraTransform.TransformVector(offset);
    }
  }
}
