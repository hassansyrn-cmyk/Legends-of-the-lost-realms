using UnityEngine;

namespace LostRealms
{
    /// Central tunables, ported 1:1 from the Android build (GameView.java / EnemyController.java).
    /// Original game ran in 1920x1080 pixel space; this port maps 1 px = 0.01 world units.
    public static class GameConfig
    {
        public const float PxToUnits = 0.01f;

        // --- Player movement (GameView.java) ---
        public const float RunSpeed = 3.10f;        // 310 px/s
        public const float GroundAccel = 22f;
        public const float AirAccel = 14f;
        public const float Gravity = 14.5f;         // 1450 px/s^2
        public const float FallGravityMultiplier = 1.72f;   // jump released while falling fast
        public const float FallGravityThreshold = 1.70f;    // vy < -170 px/s
        public const float JumpVelocity = 6.80f;    // ground jump (-680 px/s, screen-down)
        public const float DoubleJumpVelocity = 6.35f;
        public const float AirAttackBounce = 4.30f;
        public const float CoyoteTime = 0.12f;
        public const float JumpBufferTime = 0.12f;
        public const float MaxFallSpeed = 13f;

        // --- Player body ---
        public const float HeroHeight = 2.12f;      // 212 px sprite footprint
        public const float HeroHalfWidth = 0.53f;

        // --- Combat (CombatSystem.java / GameView.java) ---
        public const int MaxHealth = 5;
        public const float HurtInvuln = 0.9f;
        public const float RespawnInvuln = 1.4f;
        public const float AttackDuration = 0.24f;
        public const float ComboWindow = 0.45f;
        public const float AttackHitAt = 0.08f;     // hit frame within swing
        public const float AttackVerticalReach = 0.80f;
        public const float HitKnockback = 3.5f;
        public static readonly float[] ComboReach = { 1.04f, 1.26f, 1.26f, 1.48f };  // stage 1..4
        public static readonly int[] ComboDamage = { 1, 2, 2, 3 };                   // 1 + stageBonus (rank 0)

        // --- Enemies (EnemyController.java archetypes, level-1 tiers) ---
        public const float EnemyHitLock = 0.20f;
        public const float EnemyHurtTime = 0.22f;

        // --- Level mapping ---
        public const float LevelJsonHeight = 1080f; // worldY = (LevelJsonHeight - y) * PxToUnits
        public const float KillPlaneY = -2.5f;
        public const float LevelEndX = 22.6f;       // right edge of level 1

        public static float WorldX(float jsonX) => jsonX * PxToUnits;
        public static float WorldY(float jsonY) => (LevelJsonHeight - jsonY) * PxToUnits;
    }
}
