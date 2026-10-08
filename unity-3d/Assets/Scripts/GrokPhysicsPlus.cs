using UnityEngine;

namespace LostRealms {
  /// Visual squash/stretch on Aster's visual child only. Capsule locomotion
  /// constants stay locked.
  public sealed class GrokPhysicsPlus : MonoBehaviour {
    Vector3 visualBase = Vector3.one;
    float squash = 1f;
    bool wasGrounded = true;
    bool captured;

    void LateUpdate() {
      var g = RealmGame.I;
      if (!g || !g.Player || !g.Player.Visual) return;
      var vis = g.Player.Visual.transform;
      if (!captured) { visualBase = vis.localScale; captured = true; }
      bool grounded = g.Player.Grounded;
      if (grounded && !wasGrounded) squash = 0.86f;
      if (!grounded && wasGrounded) squash = 1.12f;
      wasGrounded = grounded;
      squash = Mathf.Lerp(squash, 1f, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
      float y = visualBase.y * squash;
      float xz = visualBase.x * (2f - squash);
      vis.localScale = new Vector3(xz, y, xz);
    }
  }
}
