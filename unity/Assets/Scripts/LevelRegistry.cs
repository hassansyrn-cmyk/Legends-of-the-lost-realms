using System.Collections.Generic;
using UnityEngine;

namespace LostRealms
{
    /// Runtime registry of level collision data. LevelBuilder fills it; player/enemies query it.
    /// One-way platforms like the original: you land when falling onto the top surface.
    public static class LevelRegistry
    {
        public struct PlatformCol
        {
            public float Left, Right, Top;
            public bool IsGroundRow;
        }

        public struct HazardCol
        {
            public float MinX, MaxX, MinY, MaxY;
        }

        public static readonly List<PlatformCol> Platforms = new List<PlatformCol>();
        public static readonly List<HazardCol> Hazards = new List<HazardCol>();
        public static Vector2 Checkpoint;
        public static bool Loaded;

        public static void Clear()
        {
            Platforms.Clear();
            Hazards.Clear();
            Loaded = false;
        }

        /// Resolve a vertical move from (x, bottomY) with velocity vy.
        /// Returns landing Y if the body crossed a platform top this frame, else null.
        public static float? LandOnPlatform(float halfWidth, float x, float prevBottom, float newBottom)
        {
            if (newBottom >= prevBottom) return null;   // only land while falling
            float best = float.NegativeInfinity;
            float? result = null;
            foreach (var p in Platforms)
            {
                if (x + halfWidth * 0.7f < p.Left || x - halfWidth * 0.7f > p.Right) continue;
                if (prevBottom >= p.Top - 0.001f && newBottom <= p.Top && p.Top > best)
                {
                    best = p.Top;
                    result = p.Top;
                }
            }
            return result;
        }

        public static bool OverlapsHazard(float minX, float maxX, float minY, float maxY)
        {
            foreach (var h in Hazards)
                if (minX < h.MaxX && maxX > h.MinX && minY < h.MaxY && maxY > h.MinY) return true;
            return false;
        }
    }
}
