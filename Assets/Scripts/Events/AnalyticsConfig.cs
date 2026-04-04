using UnityEngine;

namespace GravityRocket.Events
{
    [CreateAssetMenu(fileName = "AnalyticsConfig", menuName = "Gravity Rocket/Analytics/Config")]
    public class AnalyticsConfig : ScriptableObject
    {
        [Tooltip("All analytics event configurations")]
        public AnalyticsEventConfig[] TrackedEvents;
    }
}
