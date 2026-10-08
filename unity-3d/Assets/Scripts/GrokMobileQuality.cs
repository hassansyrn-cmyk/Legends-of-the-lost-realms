using UnityEngine;

namespace LostRealms {
  /// Mobile-first quality tiers. Never mutates locked Hero locomotion constants.
  public sealed class GrokMobileQuality : MonoBehaviour {
    public enum Tier { Low, Medium, High }
    public Tier Current { get; private set; } = Tier.High;

    public void Apply() {
      Current = Detect();
      Application.targetFrameRate = Current == Tier.Low ? 30 : 60;
      QualitySettings.vSyncCount = 0;
      Time.fixedDeltaTime = 1f / 60f;
      QualitySettings.shadowDistance = Current == Tier.High ? 36f : Current == Tier.Medium ? 22f : 12f;
      QualitySettings.shadowCascades = Current == Tier.High ? 2 : 1;
      QualitySettings.lodBias = Current == Tier.High ? 1.1f : Current == Tier.Medium ? 0.85f : 0.6f;
      QualitySettings.particleRaycastBudget = Current == Tier.Low ? 4 : 16;
      QualitySettings.maximumLODLevel = 0;
      QualitySettings.skinWeights = Current == Tier.Low ? SkinWeights.TwoBones : SkinWeights.FourBones;
      if (Current == Tier.Low) {
        QualitySettings.antiAliasing = 0;
        QualitySettings.softParticles = false;
      }
      Debug.Log("GROK_QUALITY " + Current + " gpu=" + SystemInfo.graphicsMemorySize + "MB ram=" + SystemInfo.systemMemorySize);
    }

    static Tier Detect() {
      if (Application.isEditor) return Tier.High;
      int gpu = SystemInfo.graphicsMemorySize;
      int ram = SystemInfo.systemMemorySize;
      int cores = SystemInfo.processorCount;
      if (gpu > 0 && gpu < 900) return Tier.Low;
      if (ram > 0 && ram < 3500) return Tier.Low;
      if (cores <= 4 && gpu < 1800) return Tier.Medium;
      if (gpu > 0 && gpu < 1800) return Tier.Medium;
      return Tier.High;
    }
  }
}
