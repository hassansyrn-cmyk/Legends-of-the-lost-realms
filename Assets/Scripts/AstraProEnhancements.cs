using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Manus.LostRealms
{
      public class AstraProEnhancements : MonoBehaviour
  {
          public static AstraProEnhanceme        #region 1. Graphics & Atmosphere System
  
          public enum QualityTier { Performance30, Balanced60, Ultra120 }
        public enum RealmPreset { VerdantKingdom, BurningDunes, FrozenPeaks, StormveilExpanse, AstralSanctum }

        public QualityTier CurrentQualityTier { get; private set; } = QualityTier.Balanced60;
        public RealmPreset CurrentRealm { get; private set; } = RealmPreset.VerdantKingdom;

        public struct AtmosphereSettings
{
            public Color ambientLight;
            public Color fogColor;
            public float fogDensity;
            public Color rimLightAccent;

            public AtmosphereSettings(Color ambient, Color fog, float density, Color rim)
{
                ambientLight = ambient;
                fogColor = fog;
                fogDensity = density;
                rimLightAccent = rim;
}
}

        private Dictionary<RealmPreset, AtmosphereSettings> realmPresets;

        private void InitializeGraphics()
{
            realmPresets = new Dictionary<RealmPreset, AtmosphereSettings>
{
{ RealmPreset.VerdantKingdom, new AtmosphereSettings(new Color(0.2f, 0.4f, 0.2f), new Color(0.1f, 0.3f, 0.1f), 0.015f, new Color(0.4f, 0.8f, 0.4f)) },
{ RealmPreset.BurningDunes, new AtmosphereSettings(new Color(0.5f, 0.3f, 0.1f), new Color(0.4f, 0.2f, 0.05f), 0.025f, new Color(1.0f, 0.5f, 0.1f)) },
{ RealmPreset.FrozenPeaks, new AtmosphereSettings(new Color(0.3f, 0.4f, 0.6f), new Color(0.2f, 0.3f, 0.5f), 0.02f, new Color(0.6f, 0.8f, 1.0f)) },
{ RealmPreset.StormveilExpanse, new AtmosphereSettings(new Color(0.2f, 0.2f, 0.35f), new Color(0.15f, 0.15f, 0.3f), 0.035f, new Color(0.5f, 0.5f, 1.0f)) },
{ RealmPreset.AstralSanctum, new AtmosphereSettings(new Color(0.35f, 0.15f, 0.45f), new Color(0.25f, 0.1f, 0.35f), 0.01f, new Color(0.9f, 0.4f, 1.0f)) }
};

            SetQualityTier(QualityTier.Balanced60);
            ApplyAtmospherePreset(RealmPreset.VerdantKingdom);
}

        public void SetQualityTier(QualityTier tier)
{
            CurrentQualityTier = tier;
            switch (tier)
{
  case QualityTier.Performance30:
                        Application.targetFrameRate = 30;
                        QualitySettings.v        #region 2. Combat Juice & Gameplay Depth
                          
                                  private float traverseTrauma = 0f;
            private Vector3 cameraShakeOffset = Vector3.zero;
    
            public struct FloatingTextEntry
    {
                public bool active;
                public Vector3 position;
                public string text;
                public Color color;
                public float lifetime;
    }
    
            private const int RING_BUFFER_SIZE = 64;
            private FloatingTextEntry[] textRingBuffer = new FloatingTextEntry[RING_BUFFER_SIZE];
            private int textRingIndex = 0;
    
            public enum ElementType { Ember, Frost, Gale, Astral }
    
            private void InitializeCombatJuice()
    {
                for (int i = 0; i < RING_BUFFER_SIZE; i++)
              {
                                textRingBuffer[i] = new FloatingTextEntry { active = false };
              }
}
  
          public void TriggerHitStop(float durationSeconds)
{
              StartCoroutine(HitStopRoutine(durationSeconds));
}
  
          private IEnumerator HitStopRoutine(float duration)
{
              float originalScale = Time.timeScale;
              Time.timeScale = 0.05f;
              yield return new WaitForSecondsRealtime(duration);
              Time.timeScale = originalScale;
}
  
          public void AddCameraTrauma(float amount)
{
              traverseTrauma = Mathf.Clamp01(traverseTrauma + amount);
}
  
          private void Update()
{
              UpdateCameraShake();
              UpdateFloatingText();
}
  
          private void UpdateCameraShake()
{
              if (traverseTrauma > 0)
              {
                                float shake = traverseTrauma * traverseTrauma;
                                cameraShakeOffset = new Vector3(
                                                      (UnityEngine.Random.value * 2f - 1f) * shake * 0.5f,
                                                      (UnityEngine.Random.value * 2f - 1f) * shake * 0.5f,
                                                      (UnityEngine.Random.value * 2f - 1f) * shake * 0.2f
                                                  );
                                traverseTrauma = Mathf.Max(0f, traverseTrauma - Time.deltaTime * 1.5f);
              }
              else
              {
                                cameraShakeOffset = Vector3.zero;
              }
}
  
          public string EvaluateElementalResonance(ElementType primary, ElementType secondary, ElementType? tertiary = null)
{
              if (tertiary.HasValue && primary != tertiary && secondary != tertiary)
              {
                                return "Astral Cataclysm";
              }
              if ((primary == ElementType.Ember && secondary == ElementType.Frost) || (primary == ElementType.Frost && secondary == ElementType.Ember))
                                return "Thermal Shock";
              if ((primary == ElementType.Ember && secondary == ElementType.Gale) || (primary == ElementType.Gale && secondary == ElementType.Ember))
                                return "Firestorm Vortex";
              if ((primary == ElementType.Frost && secondary == ElementType.Gale) || (primary == ElementType.Gale && secondary == ElementType.Frost))
                                return "Blizzard Tempest";
  
              return "Elemental Resonance";
}
  
          public GameObject GenerateBossTelegraphRing(Vector3 position, float radius, float duration)
{
              GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
              ring.name = "BossTelegraphRing";
              ring.transform.position = position;
              ring.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
              Destroy(ring, duration);
              return ring;
}
  
          public void SpawnFloatingCombat        #region 3. Endgame Content ("Echoes of the Lost Realms")
  
          public enum TrialTier { Tier1_Initiate, Tier2_Veteran, Tier3_Master, Tier4_Ascendant, Tier5_Mythic }
          public enum EliteAffix { Vampiric, StormShielded, ChronoHasted, VoidTethered, EmberEnraged }
  
          public struct MythicRelic
  {
              public string id;
              public string name;
              public string setGroup;
              public string description;
  
              public MythicRelic(string id, string name, string setGroup, string description)
{
                  this.id = id;
                  this.name = name;
                  this.setGroup = setGroup;
                  this.description = description;
}
}
  
          public List<MythicRelic> CollectibleRelics { get; private set; }
  
          private void InitializeEndgameContent()
{
              CollectibleRelics = new List<MythicRelic>
                {
                                new MythicRelic("relic_astral_crown", "Crown of the Astral King", "AstralDominion", "Grants +15% Elemental Power"),
                                new MythicRelic("relic_astral_orb", "Orb of Cosmic Sight", "AstralDominion", "Grants +10% Critical Chance"),
                                new MythicRelic("relic_astral_robe", "Robe of Celestial Weave", "AstralDominion", "Grants +200 Health"),
                                new MythicRelic("relic_ember_pendant", "Amulet of Infernal Fire", "EmberVanguard", "Grants +20 Fire Damage"),
                                new MythicRelic("relic_ember_blade", "Blade of Molten Heart", "EmberVanguard", "Grants +25% Attack Speed"),
                                new MythicRelic("relic_ember_ring", "Signet of Cinder", "EmberVanguard", "Grants +10% Lifesteal")
                };
}
  
          public string GetRelicSynergyBonus(string setGroup, int equippedCount)
{
              if (equippedCount >= 3)
              {
                                return $"3-Piece Set Synergy ({setGroup}): Unlocks Ultimate Elemental Burst and 30% Damage Increase!";
              }
              if (equippedCount >= 2)
              {
                                return $"2-Piece Set Synergy ({setGroup}): Unlocks +15% Primary Attribute Bonus.";
              }
              return "No active set synergy bonus.";
}
  
          public void StartAstralTrial(TrialTier tier)
{
              Debug.Log($"[AstraPro] Launching Astral Trial: {tier}");
}
  
          #endregion
  
          #region 4. Player Experience & Mobile Ergonomics
  
          public Rect CurrentSafeArea { get; private set; } = Rect.zero;
          public float TouchHudScale { get; set; } = 1.0f;
          public float TouchHudOpacity { get; set; } = 0.85f;
  
          private void InitializeMobileErgonomics()
{
              CurrentSafeArea = Screen.safeArea;
}
  
          public void TriggerHapticFeedback(int milliseconds = 50)
{
  #if UNITY_ANDROID && !UNITY_EDITOR
              try
              {
                                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                                                  using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                                                  using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                                  {
                    if (vibrator != null)
{
                          vibrator.Call("vibrate", (long)milliseconds);
}
                                }
              }
                            catch (Exception ex)
              {
                                Debug.LogWarning("[AstraPro] Haptic feedback invocation failed: " + ex.Message);
              }
  #endif
}
  
          public string GenerateSaveChecksum(string rawData)
{
              using (SHA256 sha256 = SHA256.Create())
                {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
{        #region 3. Endgame Content ("Echoes of the Lost Realms")

        public enum TrialTier { Tier1_Initiate, Tier2_Veteran, Tier3_Master, Tier4_Ascendant, Tier5_Mythic }
        public enum EliteAffix { Vampiric, StormShielded, ChronoHasted, VoidTethered, EmberEnraged }

        public struct MythicRelic
{
            public string id;
            public string name;
            public string setGroup;
            public string description;

            public MythicRelic(string id, string name, string setGroup, string description)
{
                this.id = id;
                this.name = name;
                this.setGroup = setGroup;
                this.description = description;
}
}

        public List<MythicRelic> CollectibleRelics { get; private set; }

        private void InitializeEndgameContent()
{
            CollectibleRelics = new List<MythicRelic>
{
                new MythicRelic("relic_astral_crown", "Crown of the Astral King", "AstralDominion", "Grants +15% Elemental Power"),
                new MythicRelic("relic_astral_orb", "Orb of Cosmic Sight", "AstralDominion", "Grants +10% Critical Chance"),
                new MythicRelic("relic_astral_robe", "Robe of Celestial Weave", "AstralDominion", "Grants +200 Health"),
                new MythicRelic("relic_ember_pendant", "Amulet of Infernal Fire", "EmberVanguard", "Grants +20 Fire Damage"),
                new MythicRelic("relic_ember_blade", "Blade of Molten Heart", "EmberVanguard", "Grants +25% Attack Speed"),
                new MythicRelic("relic_ember_ring", "Signet of Cinder", "EmberVanguard", "Grants +10% Lifesteal")
};
}

        public string GetRelicSynergyBonus(string setGroup, int equippedCount)
{
            if (equippedCount >= 3)
{
                return $"3-Piece Set Synergy ({setGroup}): Unlocks Ultimate Elemental Burst and 30% Damage Increase!";
}
            if (equippedCount >= 2)
{
                return $"2-Piece Set Synergy ({setGroup}): Unlocks +15% Primary Attribute Bonus.";
}
            return "No active set synergy bonus.";
}

        public void StartAstralTrial(TrialTier tier)
{
            Debug.Log($"[AstraPro] Launching Astral Trial: {tier}");
}

        #endregion

        #region 4. Player Experience & Mobile Ergonomics

        public Rect CurrentSafeArea { get; private set; } = Rect.zero;
        public float TouchHudScale { get; set; } = 1.0f;
        public float TouchHudOpacity { get; set; } = 0.85f;

        private void InitializeMobileErgonomics()
{
            CurrentSafeArea = Screen.safeArea;
}

        public void TriggerHapticFeedback(int milliseconds = 50)
{
#if UNITY_ANDROID && !UNITY_EDITOR
            try
{
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
{
                    if (vibrator != null)
{
                        vibrator.Call("vibrate", (long)milliseconds);
}
}
}
            catch (Exception ex)
{
                Debug.LogWarning("[AstraPro] Haptic feedback invocation failed: " + ex.Message);
}
#endif
}

        public string GenerateSaveChecksum(string rawData)
{
            using (SHA256 sha256 = SHA256.Create())
{
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
{        #region 3. Endgame Content ("Echoes of the Lost Realms")

        public enum TrialTier { Tier1_Initiate, Tier2_Veteran, Tier3_Master, Tier4_Ascendant, Tier5_Mythic }
        public enum EliteAffix { Vampiric, StormShielded, ChronoHasted, VoidTethered, EmberEnraged }

        public struct MythicRelic
{
            public string id;
            public string name;
            public string setGroup;
            public string description;

            public MythicRelic(string id, string name, string setGroup, string description)
{
                this.id = id;
                this.name = name;
                this.setGroup = setGroup;
                this.description = description;
}
}

        public List<MythicRelic> CollectibleRelics { get; private set; }

        private void InitializeEndgameContent()
{
            CollectibleRelics = new List<MythicRelic>
{
                new MythicRelic("relic_astral_crown", "Crown of the Astral King", "AstralDominion", "Grants +15% Elemental Power"),
                new MythicRelic("relic_astral_orb", "Orb of Cosmic Sight", "AstralDominion", "Grants +10% Critical Chance"),
                new MythicRelic("relic_astral_robe", "Robe of Celestial Weave", "AstralDominion", "Grants +200 Health"),
                new MythicRelic("relic_ember_pendant", "Amulet of Infernal Fire", "EmberVanguard", "Grants +20 Fire Damage"),
                new MythicRelic("relic_ember_blade", "Blade of Molten Heart", "EmberVanguard", "Grants +25% Attack Speed"),
                new MythicRelic("relic_ember_ring", "Signet of Cinder", "EmberVanguard", "Grants +10% Lifesteal")
};
}

        public string GetRelicSynergyBonus(string setGroup, int equippedCount)
{
            if (equippedCount >= 3)
{
                return $"3-Piece Set Synergy ({setGroup}): Unlocks Ultimate Elemental Burst and 30% Damage Increase!";
}
            if (equippedCount >= 2)
{
                return $"2-Piece Set Synergy ({setGroup}): Unlocks +15% Primary Attribute Bonus.";
}
            return "No active set synergy bonus.";
}

        public void StartAstralTrial(TrialTier tier)
{
            Debug.Log($"[AstraPro] Launching Astral Trial: {tier}");
}

        #endregion

        #region 4. Player Experience & Mobile Ergonomics

        public Rect CurrentSafeArea { get; private set; } = Rect.zero;
        public float TouchHudScale { get; set; } = 1.0f;
        public float TouchHudOpacity { get; set; } = 0.85f;

        private void InitializeMobileErgonomics()
{
            CurrentSafeArea = Screen.safeArea;
}

        public void TriggerHapticFeedback(int milliseconds = 50)
{
#if UNITY_ANDROID && !UNITY_EDITOR
            try
{
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
{
                    if (vibrator != null)
{
                        vibrator.Call("vibrate", (long)milliseconds);
}
}
}
            catch (Exception ex)
{
                Debug.LogWarning("[AstraPro] Haptic feedback invocation failed: " + ex.Message);
}
#endif
}

        public string GenerateSaveChecksum(string rawData)
{
            using (SHA256 sha256 = SHA256.Create())
{
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
{        #region 3. Endgame Content ("Echoes of the Lost Realms")

        public enum TrialTier { Tier1_Initiate, Tier2_Veteran, Tier3_Master, Tier4_Ascendant, Tier5_Mythic }
        public enum EliteAffix { Vampiric, StormShielded, ChronoHasted, VoidTethered, EmberEnraged }

        public struct MythicRelic
{
            public string id;
            public string name;
            public string setGroup;
            public string description;

            public MythicRelic(string id, string name, string setGroup, string description)
{
                this.id = id;
                this.name = name;
                this.setGroup = setGroup;
                this.description = description;
}
}

        public List<MythicRelic> CollectibleRelics { get; private set; }

        private void InitializeEndgameContent()
{
            CollectibleRelics = new List<MythicRelic>
{
                new MythicRelic("relic_astral_crown", "Crown of the Astral King", "AstralDominion", "Grants +15% Elemental Power"),
                new MythicRelic("relic_astral_orb", "Orb of Cosmic Sight", "AstralDominion", "Grants +10% Critical Chance"),
                new MythicRelic("relic_astral_robe", "Robe of Celestial Weave", "AstralDominion", "Grants +200 Health"),
                new MythicRelic("relic_ember_pendant", "Amulet of Infernal Fire", "EmberVanguard", "Grants +20 Fire Damage"),
                new MythicRelic("relic_ember_blade", "Blade of Molten Heart", "EmberVanguard", "Grants +25% Attack Speed"),
                new MythicRelic("relic_ember_ring", "Signet of Cinder", "EmberVanguard", "Grants +10% Lifesteal")
};
}

        public string GetRelicSynergyBonus(string setGroup, int equippedCount)
{
            if (equippedCount >= 3)
{
                return $"3-Piece Set Synergy ({setGroup}): Unlocks Ultimate Elemental Burst and 30% Damage Increase!";
}
            if (equippedCount >= 2)
{
                return $"2-Piece Set Synergy ({setGroup}): Unlocks +15% Primary Attribute Bonus.";
}
            return "No active set synergy bonus.";
}

        public void StartAstralTrial(TrialTier tier)
{
            Debug.Log($"[AstraPro] Launching Astral Trial: {tier}");
}

        #endregion

        #region 4. Player Experience & Mobile Ergonomics

        public Rect CurrentSafeArea { get; private set; } = Rect.zero;
        public float TouchHudScale { get; set; } = 1.0f;
        public float TouchHudOpacity { get; set; } = 0.85f;

        private void InitializeMobileErgonomics()
{
            CurrentSafeArea = Screen.safeArea;
}

        public void TriggerHapticFeedback(int milliseconds = 50)
{
#if UNITY_ANDROID && !UNITY_EDITOR
            try
{
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
{
                    if (vibrator != null)
{
                        vibrator.Call("vibrate", (long)milliseconds);
}
}
}
            catch (Exception ex)
{
                Debug.LogWarning("[AstraPro] Haptic feedback invocation failed: " + ex.Message);
}
#endif
}

        public string GenerateSaveChecksum(string rawData)
{
            using (SHA256 sha256 = SHA256.Create())
{
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
{        #region 3. Endgame Content ("Echoes of the Lost Realms")

        public enum TrialTier { Tier1_Initiate, Tier2_Veteran, Tier3_Master, Tier4_Ascendant, Tier5_Mythic }
        public enum EliteAffix { Vampiric, StormShielded, ChronoHasted, VoidTethered, EmberEnraged }

        public struct MythicRelic
{
            public string id;
            public string name;
            public string setGroup;
            public string description;

            public MythicRelic(string id, string name, string setGroup, string description)
{
                this.id = id;
                this.name = name;
                this.setGroup = setGroup;
                this.description = description;
}
}

        public List<MythicRelic> CollectibleRelics { get; private set; }

        private void InitializeEndgameContent()
{
            CollectibleRelics = new List<MythicRelic>
{
                new MythicRelic("relic_astral_crown", "Crown of the Astral King", "AstralDominion", "Grants +15% Elemental Power"),
                new MythicRelic("relic_astral_orb", "Orb of Cosmic Sight", "AstralDominion", "Grants +10% Critical Chance"),
                new MythicRelic("relic_astral_robe", "Robe of Celestial Weave", "AstralDominion", "Grants +200 Health"),
                new MythicRelic("relic_ember_pendant", "Amulet of Infernal Fire", "EmberVanguard", "Grants +20 Fire Damage"),
                new MythicRelic("relic_ember_blade", "Blade of Molten Heart", "EmberVanguard", "Grants +25% Attack Speed"),
                new MythicRelic("relic_ember_ring", "Signet of Cinder", "EmberVanguard", "Grants +10% Lifesteal")
};
}

        public string GetRelicSynergyBonus(string setGroup, int equippedCount)
{
            if (equippedCount >= 3)
{
                return $"3-Piece Set Synergy ({setGroup}): Unlocks Ultimate Elemental Burst and 30% Damage Increase!";
}
            if (equippedCount >= 2)
{
                return $"2-Piece Set Synergy ({setGroup}): Unlocks +15% Primary Attribute Bonus.";
}
            return "No active set synergy bonus.";
}

        public void StartAstralTrial(TrialTier tier)
{
            Debug.Log($"[AstraPro] Launching Astral Trial: {tier}");
}

        #endregion

        #region 4. Player Experience & Mobile Ergonomics

        public Rect CurrentSafeArea { get; private set; } = Rect.zero;
        public float TouchHudScale { get; set; } = 1.0f;
        public float TouchHudOpacity { get; set; } = 0.85f;

        private void InitializeMobileErgonomics()
{
            CurrentSafeArea = Screen.safeArea;
}

        public void TriggerHapticFeedback(int milliseconds = 50)
{
#if UNITY_ANDROID && !UNITY_EDITOR
            try
{
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
{
                    if (vibrator != null)
{
                        vibrator.Call("vibrate", (long)milliseconds);
}
}
}
            catch (Exception ex)
{
                Debug.LogWarning("[AstraPro] Haptic feedback invocation failed: " + ex.Message);
}
#endif
}

        public string GenerateSaveChecksum(string rawData)
{
            using (SHA256 sha256 = SHA256.Create())
{
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
{        #region 3. Endgame Content ("Echoes of the Lost Realms")

        public enum TrialTier { Tier1_Initiate, Tier2_Veteran, Tier3_Master, Tier4_Ascendant, Tier5_Mythic }
        public enum EliteAffix { Vampiric, StormShielded, ChronoHasted, VoidTethered, EmberEnraged }

        public struct MythicRelic
{
            public string id;
            public string name;
            public string setGroup;
            public string description;

            public MythicRelic(string id, string name, string setGroup, string description)
{
                this.id = id;
                this.name = name;
                this.setGroup = setGroup;
                this.description = description;
}
}

        public List<MythicRelic> CollectibleRelics { get; private set; }

        private void InitializeEndgameContent()
{
            CollectibleRelics = new List<MythicRelic>
{
                new MythicRelic("relic_astral_crown", "Crown of the Astral King", "AstralDominion", "Grants +15% Elemental Power"),
                new MythicRelic("relic_astral_orb", "Orb of Cosmic Sight", "AstralDominion", "Grants +10% Critical Chance"),
                new MythicRelic("relic_astral_robe", "Robe of Celestial Weave", "AstralDominion", "Grants +200 Health"),
                new MythicRelic("relic_ember_pendant", "Amulet of Infernal Fire", "EmberVanguard", "Grants +20 Fire Damage"),
                new MythicRelic("relic_ember_blade", "Blade of Molten Heart", "EmberVanguard", "Grants +25% Attack Speed"),
                new MythicRelic("relic_ember_ring", "Signet of Cinder", "EmberVanguard", "Grants +10% Lifesteal")
};
}

        public string GetRelicSynergyBonus(string setGroup, int equippedCount)
{
            if (equippedCount >= 3)
{
                return $"3-Piece Set Synergy ({setGroup}): Unlocks Ultimate Elemental Burst and 30% Damage Increase!";
}
            if (equippedCount >= 2)
{
                return $"2-Piece Set Synergy ({setGroup}): Unlocks +15% Primary Attribute Bonus.";
}
            return "No active set synergy bonus.";
}

        public void StartAstralTrial(TrialTier tier)
{
            Debug.Log($"[AstraPro] Launching Astral Trial: {tier}");
}

        #endregion

        #region 4. Player Experience & Mobile Ergonomics

        public Rect CurrentSafeArea { get; private set; } = Rect.zero;
        public float TouchHudScale { get; set; } = 1.0f;
        public float TouchHudOpacity { get; set; } = 0.85f;

        private void InitializeMobileErgonomics()
{
            CurrentSafeArea = Screen.safeArea;
}

        public void TriggerHapticFeedback(int milliseconds = 50)
{
#if UNITY_ANDROID && !UNITY_EDITOR
            try
{
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
{
                    if (vibrator != null)
{
                        vibrator.Call("vibrate", (long)milliseconds);
}
}
}
            catch (Exception ex)
{
                Debug.LogWarning("[AstraPro] Haptic feedback invocation failed: " + ex.Message);
}
#endif
}

        public string GenerateSaveChecksum(string rawData)
{
            using (SHA256 sha256 = SHA256.Create())
{
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
{        #region 3. Endgame Content ("Echoes of the Lost Realms")

        public enum TrialTier { Tier1_Initiate, Tier2_Veteran, Tier3_Master, Tier4_Ascendant, Tier5_Mythic }
        public enum EliteAffix { Vampiric, StormShielded, ChronoHasted, VoidTethered, EmberEnraged }

        public struct MythicRelic
{
            public string id;
            public string name;
            public string setGroup;
            public string description;

            public MythicRelic(string id, string name, string setGroup, string description)
{
                this.id = id;
                this.name = name;
                this.setGroup = setGroup;
                this.description = description;
}
}

        public List<MythicRelic> CollectibleRelics { get; private set; }

        private void InitializeEndgameContent()
{
            CollectibleRelics = new List<MythicRelic>
{
                new MythicRelic("relic_astral_crown", "Crown of the Astral King", "AstralDominion", "Grants +15% Elemental Power"),
                new MythicRelic("relic_astral_orb", "Orb of Cosmic Sight", "AstralDominion", "Grants +10% Critical Chance"),
                new MythicRelic("relic_astral_robe", "Robe of Celestial Weave", "AstralDominion", "Grants +200 Health"),
                new MythicRelic("relic_ember_pendant", "Amulet of Infernal Fire", "EmberVanguard", "Grants +20 Fire Damage"),
                new MythicRelic("relic_ember_blade", "Blade of Molten Heart", "EmberVanguard", "Grants +25% Attack Speed"),
                new MythicRelic("relic_ember_ring", "Signet of Cinder", "EmberVanguard", "Grants +10% Lifesteal")
};
}

        public string GetRelicSynergyBonus(string setGroup, int equippedCount)
{
            if (equippedCount >= 3)
{
                return $"3-Piece Set Synergy ({setGroup}): Unlocks Ultimate Elemental Burst and 30% Damage Increase!";
}
            if (equippedCount >= 2)
{
                return $"2-Piece Set Synergy ({setGroup}): Unlocks +15% Primary Attribute Bonus.";
}
            return "No active set synergy bonus.";
}

        public void StartAstralTrial(TrialTier tier)
{
            Debug.Log($"[AstraPro] Launching Astral Trial: {tier}");
}

        #endregion

        #region 4. Player Experience & Mobile Ergonomics

        public Rect CurrentSafeArea { get; private set; } = Rect.zero;
        public float TouchHudScale { get; set; } = 1.0f;
        public float TouchHudOpacity { get; set; } = 0.85f;

        private void InitializeMobileErgonomics()
{
            CurrentSafeArea = Screen.safeArea;
}

        public void TriggerHapticFeedback(int milliseconds = 50)
{
#if UNITY_ANDROID && !UNITY_EDITOR
            try
{
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
{
                    if (vibrator != null)
{
                        vibrator.Call("vibrate", (long)milliseconds);
}
}
}
            catch (Exception ex)
{
                Debug.LogWarning("[AstraPro] Haptic feedback invocation failed: " + ex.Message);
}
#endif
}

        public string GenerateSaveChecksum(string rawData)
{
            using (SHA256 sha256 = SHA256.Create())
{
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
{        #region 3. Endgame Content ("Echoes of the Lost Realms")

        public enum TrialTier { Tier1_Initiate, Tier2_Veteran, Tier3_Master, Tier4_Ascendant, Tier5_Mythic }
        public enum EliteAffix { Vampiric, StormShielded, ChronoHasted, VoidTethered, EmberEnraged }

        public struct MythicRelic
{
            public string id;
            public string name;
            public string setGroup;
            public string description;

            public MythicRelic(string id, string name, string setGroup, string description)
{
                this.id = id;
                this.name = name;
                this.setGroup = setGroup;
                this.description = description;
}
}

        public List<MythicRelic> CollectibleRelics { get; private set; }

        private void InitializeEndgameContent()
{
            CollectibleRelics = new List<MythicRelic>
{
                new MythicRelic("relic_astral_crown", "Crown of the Astral King", "AstralDominion", "Grants +15% Elemental Power"),
                new MythicRelic("relic_astral_orb", "Orb of Cosmic Sight", "AstralDominion", "Grants +10% Critical Chance"),
                new MythicRelic("relic_astral_robe", "Robe of Celestial Weave", "AstralDominion", "Grants +200 Health"),
                new MythicRelic("relic_ember_pendant", "Amulet of Infernal Fire", "EmberVanguard", "Grants +20 Fire Damage"),
                new MythicRelic("relic_ember_blade", "Blade of Molten Heart", "EmberVanguard", "Grants +25% Attack Speed"),
                new MythicRelic("relic_ember_ring", "Signet of Cinder", "EmberVanguard", "Grants +10% Lifesteal")
};
}

        public string GetRelicSynergyBonus(string setGroup, int equippedCount)
{
            if (equippedCount >= 3)
{
                return $"3-Piece Set Synergy ({setGroup}): Unlocks Ultimate Elemental Burst and 30% Damage Increase!";
}
            if (equippedCount >= 2)
{
                return $"2-Piece Set Synergy ({setGroup}): Unlocks +15% Primary Attribute Bonus.";
}
            return "No active set synergy bonus.";
}

        public void StartAstralTrial(TrialTier tier)
{
            Debug.Log($"[AstraPro] Launching Astral Trial: {tier}");
}

        #endregion

        #region 4. Player Experience & Mobile Ergonomics

        public Rect CurrentSafeArea { get; private set; } = Rect.zero;
        public float TouchHudScale { get; set; } = 1.0f;
        public float TouchHudOpacity { get; set; } = 0.85f;

        private void InitializeMobileErgonomics()
{
            CurrentSafeArea = Screen.safeArea;
}

        public void TriggerHapticFeedback(int milliseconds = 50)
{
#if UNITY_ANDROID && !UNITY_EDITOR
            try
{
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
{
                    if (vibrator != null)
{
                        vibrator.Call("vibrate", (long)milliseconds);
}
}
}
            catch (Exception ex)
{
                Debug.LogWarning("[AstraPro] Haptic feedback invocation failed: " + ex.Message);
}
#endif
}

        public string GenerateSaveChecksum(string rawData)
{
            using (SHA256 sha256 = SHA256.Create())
{
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
{
                    builder.Append(bytes[i].ToString("x2"));
}
                return builder.ToString();
}
}

        public bool VerifyAndRecoverSave(string rawData, string storedChecksum, out string recoveredData)
{
            string computed = GenerateSaveChecksum(rawData);
            if (computed.Equals(storedChecksum, StringComparison.OrdinalIgnoreCase))
{
                recoveredData = rawData;
                return true;
}
            Debug.LogWarning("[AstraPro] Primary save checksum mismatch. Attempting backup slot recovery.");
            recoveredData = PlayerPrefs.GetString("BackupSaveSlot_Data", string.Empty);
            string backupChecksum = PlayerPrefs.GetString("BackupSaveSlot_Checksum", string.Empty);

            if (!string.IsNullOrEmpty(recoveredData) && GenerateSaveChecksum(recoveredData).Equals(backupChecksum, StringComparison.OrdinalIgnoreCase))
{
                return true;
}

            recoveredData = null;
            return false;
}

        #endregion
}
}
                    builder.Append(bytes[i].ToString("x2"));
}
                return builder.ToString();
}
}

        public bool VerifyAndRecoverSave(string rawData, string storedChecksum, out string recoveredData)
{
            string computed = GenerateSaveChecksum(rawData);
            if (computed.Equals(storedChecksum, StringComparison.OrdinalIgnoreCase))
{
                recoveredData = rawData;
                return true;
}
            Debug.LogWarning("[AstraPro] Primary save checksum mismatch. Attempting backup slot recovery.");
            recoveredData = PlayerPrefs.GetString("BackupSaveSlot_Data", string.Empty);
            string backupChecksum = PlayerPrefs.GetString("BackupSaveSlot_Checksum", string.Empty);

            if (!string.IsNullOrEmpty(recoveredData) && GenerateSaveChecksum(recoveredData).Equals(backupChecksum, StringComparison.OrdinalIgnoreCase))
{
                return true;
}

            recoveredData = null;
            return false;
}

        #endregion
}
}
                    builder.Append(bytes[i].ToString("x2"));
}
                return builder.ToString();
}
}

        public bool VerifyAndRecoverSave(string rawData, string storedChecksum, out string recoveredData)
{
            string computed = GenerateSaveChecksum(rawData);
            if (computed.Equals(storedChecksum, StringComparison.OrdinalIgnoreCase))
{
                recoveredData = rawData;
                return true;
}
            Debug.LogWarning("[AstraPro] Primary save checksum mismatch. Attempting backup slot recovery.");
            recoveredData = PlayerPrefs.GetString("BackupSaveSlot_Data", string.Empty);
            string backupChecksum = PlayerPrefs.GetString("BackupSaveSlot_Checksum", string.Empty);

            if (!string.IsNullOrEmpty(recoveredData) && GenerateSaveChecksum(recoveredData).Equals(backupChecksum, StringComparison.OrdinalIgnoreCase))
{
                return true;
}

            recoveredData = null;
            return false;
}

        #endregion
}
}
                    builder.Append(bytes[i].ToString("x2"));
}
                return builder.ToString();
}
}

        public bool VerifyAndRecoverSave(string rawData, string storedChecksum, out string recoveredData)
{
            string computed = GenerateSaveChecksum(rawData);
            if (computed.Equals(storedChecksum, StringComparison.OrdinalIgnoreCase))
{
                recoveredData = rawData;
                return true;
}
            Debug.LogWarning("[AstraPro] Primary save checksum mismatch. Attempting backup slot recovery.");
            recoveredData = PlayerPrefs.GetString("BackupSaveSlot_Data", string.Empty);
            string backupChecksum = PlayerPrefs.GetString("BackupSaveSlot_Checksum", string.Empty);

            if (!string.IsNullOrEmpty(recoveredData) && GenerateSaveChecksum(recoveredData).Equals(backupChecksum, StringComparison.OrdinalIgnoreCase))
{
                return true;
}

            recoveredData = null;
            return false;
}

        #endregion
}
}
                    builder.Append(bytes[i].ToString("x2"));
}
                return builder.ToString();
}
}

        public bool VerifyAndRecoverSave(string rawData, string storedChecksum, out string recoveredData)
{
            string computed = GenerateSaveChecksum(rawData);
            if (computed.Equals(storedChecksum, StringComparison.OrdinalIgnoreCase))
{
                recoveredData = rawData;
                return true;
}
            Debug.LogWarning("[AstraPro] Primary save checksum mismatch. Attempting backup slot recovery.");
            recoveredData = PlayerPrefs.GetString("BackupSaveSlot_Data", string.Empty);
            string backupChecksum = PlayerPrefs.GetString("BackupSaveSlot_Checksum", string.Empty);

            if (!string.IsNullOrEmpty(recoveredData) && GenerateSaveChecksum(recoveredData).Equals(backupChecksum, StringComparison.OrdinalIgnoreCase))
{
                return true;
}

            recoveredData = null;
            return false;
}

        #endregion
}
}
                    builder.Append(bytes[i].ToString("x2"));
}
                return builder.ToString();
}
}

        public bool VerifyAndRecoverSave(string rawData, string storedChecksum, out string recoveredData)
{
            string computed = GenerateSaveChecksum(rawData);
            if (computed.Equals(storedChecksum, StringComparison.OrdinalIgnoreCase))
{
                recoveredData = rawData;
                return true;
}
            Debug.LogWarning("[AstraPro] Primary save checksum mismatch. Attempting backup slot recovery.");
            recoveredData = PlayerPrefs.GetString("BackupSaveSlot_Data", string.Empty);
            string backupChecksum = PlayerPrefs.GetString("BackupSaveSlot_Checksum", string.Empty);

            if (!string.IsNullOrEmpty(recoveredData) && GenerateSaveChecksum(recoveredData).Equals(backupChecksum, StringComparison.OrdinalIgnoreCase))
{
                return true;
}

            recoveredData = null;
            return false;
}

        #endregion
}
}
                    builder.Append(bytes[i].ToString("x2"));
}
                return builder.ToString();
}
}

        public bool VerifyAndRecoverSave(string rawData, string storedChecksum, out string recoveredData)
{
            string computed = GenerateSaveChecksum(rawData);
            if (computed.Equals(storedChecksum, StringComparison.OrdinalIgnoreCase))
{
                recoveredData = rawData;
                return true;
}
            Debug.LogWarning("[AstraPro] Primary save checksum mismatch. Attempting backup slot recovery.");
            recoveredData = PlayerPrefs.GetString("BackupSaveSlot_Data", string.Empty);
            string backupChecksum = PlayerPrefs.GetString("BackupSaveSlot_Checksum", string.Empty);

            if (!string.IsNullOrEmpty(recoveredData) && GenerateSaveChecksum(recoveredData).Equals(backupChecksum, StringComparison.OrdinalIgnoreCase))
{
                return true;
}

            recoveredData = null;
            return false;
}

        #endregion
}
}
                    builder.Append(bytes[i].ToString("x2"));
}
                return builder.ToString();
}
}

        public bool VerifyAndRecoverSave(string rawData, string storedChecksum, out string recoveredData)
{
            string computed = GenerateSaveChecksum(rawData);
            if (computed.Equals(storedChecksum, StringComparison.OrdinalIgnoreCase))
{
                recoveredData = rawData;
                return true;
}
            Debug.LogWarning("[AstraPro] Primary save checksum mismatch. Attempting backup slot recovery.");
            recoveredData = PlayerPrefs.GetString("BackupSaveSlot_Data", string.Empty);
            string backupChecksum = PlayerPrefs.GetString("BackupSaveSlot_Checksum", string.Empty);

            if (!string.IsNullOrEmpty(recoveredData) && GenerateSaveChecksum(recoveredData).Equals(backupChecksum, StringComparison.OrdinalIgnoreCase))
{
                return true;
}

            recoveredData = null;
            return false;
}

        #endregion
}
}Text(Vector3 pos, string txt, Color col, float duration = 1.0f)
{
              textRingBuffer[textRingIndex] = new FloatingTextEntry
                {
                                active = true,
                                position = pos,
                                text = txt,
                                color = col,
                                lifetime = duration
};
              textRingIndex = (textRingIndex + 1) % RING_BUFFER_SIZE;
}
  
          private void UpdateFloatingText()
{
              for (int i = 0; i < RING_BUFFER_SIZE; i++)
                {
                                  if (textRingBuffer[i].active)
                                  {
                                                        textRingBuffer[i].lifetime -= Time.deltaTime;
                                                        textRingBuffer[i].position += Vector3.up * (0.5f * Time.deltaTime);
                                                        if (textRingBuffer[i].lifetime <= 0)
                                                        {
                                                                                  textRingBuffer[i].active = false;
                                                        }
                                  }
                }
}
  
          #endregion
  SyncCount = 0;
                        QualitySettings.shadowDistance = 20f;
                        break;
  case QualityTier.Balanced60:
                        Application.targetFrameRate = 60;
                        QualitySettings.vSyncCount = 1;
                        QualitySettings.shadowDistance = 50f;
                        break;
  case QualityTier.Ultra120:
                        Application.targetFrameRate = 120;
                        QualitySettings.vSyncCount = 0;
                        QualitySettings.shadowDistance = 100f;
                        break;
}
}

        public void ApplyAtmospherePreset(RealmPreset realm)
{
            CurrentRealm = realm;
            if (realmPresets.TryGetValue(realm, out AtmosphereSettings settings))
{
                RenderSettings.ambientLight = settings.ambientLight;
                RenderSettings.fog = true;
                RenderSettings.fogColor = settings.fogColor;
                RenderSettings.fogDensity = settings.fogDensity;
                Shader.SetGlobalColor("_RimLightAccent", settings.rimLightAccent);
}
}

        #endregion
nts Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
                  private static void AutoInitialize()
{
              if (Instance == null)
{
                GameObject go = new GameObject("AstraProEnhancementsContainer");
                Instance = go.AddComponent<AstraProEnhancements>();
                DontDestroyOnLoad(go);
}
}

        private void Awake()
{
              if (Instance != null && Instance != this)
{
                Destroy(gameObject);
                return;
}
            Instance = this;
            InitializeModules();
}

        private void InitializeModules()
{
              InitializeGraphics();
            InitializeMobileErgonomics();
            InitializeCombatJuice();
            InitializeEndgameContent();
}
