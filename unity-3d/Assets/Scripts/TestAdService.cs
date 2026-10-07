using System;
using UnityEngine;

namespace LostRealms {
 public interface IRewardedAdService {
  bool IsReady { get; }
  void ShowRewardedAd(Action onRewardEarned, Action onAdClosed = null);
 }

 // High-fidelity simulated rewarded ad provider ready for replacement with Google AdMob or Unity Ads.
 public static class RewardedAdManager {
  public static bool IsShowing { get; private set; }
  public static float TimeRemaining { get; private set; }
  public const float AdDuration = 4.0f;

  static Action onReward;
  static Action onClosed;

  public static void ShowRewardedAd(Action rewardEarned, Action adClosed = null) {
   onReward = rewardEarned;
   onClosed = adClosed;
   TimeRemaining = AdDuration;
   IsShowing = true;
  }

  public static void Update(float dt) {
   if (!IsShowing) return;
   if (TimeRemaining > 0f) {
    TimeRemaining = Mathf.Max(0f, TimeRemaining - dt);
   }
  }

  public static void Claim() {
   if (!IsShowing) return;
   IsShowing = false;
   var cb = onReward;
   onReward = null;
   onClosed = null;
   cb?.Invoke();
  }

  public static void Close() {
   if (!IsShowing) return;
   IsShowing = false;
   var cb = onClosed;
   onReward = null;
   onClosed = null;
   cb?.Invoke();
  }

  public static void DrawGUI(Texture2D pixel, GUIStyle titleC, GUIStyle labelC, GUIStyle btnText) {
   if (!IsShowing) return;

   // Dark modal backdrop
   GUI.color = new Color(0.02f, 0.04f, 0.06f, 0.95f);
   GUI.DrawTexture(new Rect(0, 0, 1280, 720), pixel);
   GUI.color = Color.white;

   // Ad window
   Rect panel = new Rect(280, 80, 720, 560);
   GUI.color = new Color(0.06f, 0.10f, 0.16f, 0.98f);
   GUI.DrawTexture(panel, pixel);
   GUI.color = new Color(0.38f, 0.95f, 0.7f, 0.9f);
   // Border
   GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 3), pixel);
   GUI.DrawTexture(new Rect(panel.x, panel.yMax - 3, panel.width, 3), pixel);
   GUI.DrawTexture(new Rect(panel.x, panel.y, 3, panel.height), pixel);
   GUI.DrawTexture(new Rect(panel.xMax - 3, panel.y, 3, panel.height), pixel);
   GUI.color = Color.white;

   // Ad header
   GUI.color = new Color(0.75f, 0.85f, 0.95f);
   GUI.Label(new Rect(panel.x + 20, panel.y + 20, panel.width - 40, 24), "TEST REWARDED ADVERTISEMENT  •  GOOGLE ADMOB / UNITY ADS PREVIEW", labelC);
   GUI.color = new Color(1f, 0.88f, 0.45f);
   GUI.Label(new Rect(panel.x + 20, panel.y + 54, panel.width - 40, 40), "Legends of the Lost Realms", titleC);
   GUI.color = Color.white;

   // Ad card art
   Rect card = new Rect(panel.x + 40, panel.y + 110, panel.width - 80, 240);
   GUI.color = new Color(0.10f, 0.16f, 0.24f, 0.95f);
   GUI.DrawTexture(card, pixel);
   GUI.color = new Color(0.4f, 0.83f, 1f, 0.6f);
   GUI.DrawTexture(new Rect(card.x, card.y, card.width, 2), pixel);
   GUI.DrawTexture(new Rect(card.x, card.yMax - 2, card.width, 2), pixel);
   GUI.color = Color.white;

   GUI.Label(new Rect(card.x + 20, card.y + 30, card.width - 40, 32), "✦ UNLOCK THE FULL EXPEDITION ✦", titleC);
   GUI.color = new Color(0.85f, 0.92f, 0.98f);
   GUI.Label(new Rect(card.x + 30, card.y + 80, card.width - 60, 60), "Master 49 unique elemental blades, spears, and axes.\nConquer 15 floating island chapters and vanquish all four realm titans.", labelC);
   GUI.color = new Color(0.38f, 0.95f, 0.7f);
   GUI.Label(new Rect(card.x + 30, card.y + 160, card.width - 60, 30), "Thank you for supporting Legends of the Lost Realms!", labelC);
   GUI.color = Color.white;

   // Progress bar
   float progress = 1f - (TimeRemaining / AdDuration);
   Rect barBg = new Rect(panel.x + 50, panel.y + 380, panel.width - 100, 16);
   GUI.color = new Color(0.04f, 0.08f, 0.12f, 0.9f);
   GUI.DrawTexture(barBg, pixel);
   if (progress > 0f) {
    GUI.color = progress >= 1f ? new Color(0.38f, 0.95f, 0.7f) : new Color(0.4f, 0.85f, 1f);
    GUI.DrawTexture(new Rect(barBg.x, barBg.y, barBg.width * progress, barBg.height), pixel);
   }
   GUI.color = Color.white;

   bool ready = TimeRemaining <= 0f;
   string statusText = ready ? "✓ REWARD READY! Tap below to claim your Free Normal Chest." : $"Watching ad... Reward unlocks in {Mathf.CeilToInt(TimeRemaining)}s";
   GUI.Label(new Rect(panel.x + 50, panel.y + 406, panel.width - 100, 24), statusText, labelC);

   // Action buttons
   if (ready) {
    GUI.color = new Color(0.38f, 0.95f, 0.7f);
    Rect claimBtn = new Rect(panel.x + 160, panel.y + 460, 400, 56);
    GUI.DrawTexture(claimBtn, pixel);
    GUI.color = new Color(0.02f, 0.05f, 0.08f);
    GUI.Label(claimBtn, "CLAIM FREE CHEST", btnText);
    GUI.color = Color.white;
    if (GUI.Button(claimBtn, GUIContent.none, GUIStyle.none)) {
     Claim();
    }
   } else {
    Rect closeBtn = new Rect(panel.x + 230, panel.y + 470, 260, 44);
    GUI.color = new Color(0.2f, 0.25f, 0.3f, 0.8f);
    GUI.DrawTexture(closeBtn, pixel);
    GUI.color = new Color(0.8f, 0.85f, 0.9f);
    GUI.Label(closeBtn, "CANCEL (NO REWARD)", labelC);
    GUI.color = Color.white;
    if (GUI.Button(closeBtn, GUIContent.none, GUIStyle.none)) {
     Close();
    }
   }
  }
 }
}
