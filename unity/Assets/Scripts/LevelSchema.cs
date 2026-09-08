using System;
using System.Collections.Generic;

namespace LostRealms
{
    [Serializable]
    public class LevelSchema
    {
        public int id;
        public string stageName;
        public string story;
        public Checkpoint checkpoint;
        public List<Platform> platforms;
        public List<Pickup> pickups;
        public List<Foe> foes;
        public List<Hazard> hazards;

        [Serializable] public class Checkpoint { public float x, y; }
        [Serializable] public class Platform { public float x, y, width; public bool crumble; public string material; public float moveX, moveY, moveSpeed; }
        [Serializable] public class Pickup { public float x, y; public bool gem; public string route; }
        [Serializable] public class Foe { public float x, y; public int kind; }
        [Serializable] public class Hazard { public float left, top, right, bottom; }
    }
}
