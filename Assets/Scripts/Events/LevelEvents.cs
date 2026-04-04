using System;
using UnityEngine;

namespace GravityRocket.Events
{
    [Serializable]
    public struct LevelEventData
    {
        public int LevelIndex;
        public int LevelType;
        public int ShieldsRemaining;
    }

    [Serializable]
    public struct LevelCompleteData
    {
        public int LevelIndex;
        public int LevelType;
        public int StarsEarned;
        public int ShieldsRemaining;
        public bool IsNewBest;
    }

    [Serializable]
    public struct LevelFailData
    {
        public int LevelIndex;
        public int LevelType;
        public string FailReason;
        public int ShieldsRemaining;
    }

    [CreateAssetMenu(fileName = "LevelStartEvent", menuName = "Gravity Rocket/Events/Level Start Event")]
    public class LevelStartEvent : GameEvent<LevelEventData> { }

    [CreateAssetMenu(fileName = "LevelCompleteEvent", menuName = "Gravity Rocket/Events/Level Complete Event")]
    public class LevelCompleteEvent : GameEvent<LevelCompleteData> { }

    [CreateAssetMenu(fileName = "LevelFailEvent", menuName = "Gravity Rocket/Events/Level Fail Event")]
    public class LevelFailEvent : GameEvent<LevelFailData> { }
}
