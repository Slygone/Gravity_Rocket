using UnityEngine;

namespace GravityRocket.Events
{
    [CreateAssetMenu(fileName = "AnalyticsEventConfig", menuName = "Gravity Rocket/Analytics/Event Config")]
    public class AnalyticsEventConfig : ScriptableObject
    {
        [Tooltip("The analytics event name sent to the backend (e.g. 'level_start')")]
        public string EventName;

        [Tooltip("Which event channel this tracks")]
        public EventChannelType ChannelType;
    }

    public enum EventChannelType
    {
        LevelStart,
        LevelComplete,
        LevelFail
    }
}
