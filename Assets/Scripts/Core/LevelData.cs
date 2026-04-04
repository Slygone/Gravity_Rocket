using System;
using UnityEngine;

namespace GravityRocket.Core
{
    [Serializable]
    public struct PlanetConfig
    {
        public Vector2 Position;
        public float Radius;
        public float Mass;
    }

    [Serializable]
    public struct LevelData
    {
        public int LevelIndex;
        public int LayoutType;
        public Vector2 StartPosition;
        public Vector2 GoalPosition;
        public float GoalWidth;
        public PlanetConfig[] Planets;
    }
}
