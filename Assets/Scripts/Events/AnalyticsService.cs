using UnityEngine;
using System.Collections.Generic;

namespace GravityRocket.Events
{
    public class AnalyticsService : MonoBehaviour
    {
        [SerializeField] private AnalyticsConfig _config;
        [SerializeField] private LevelStartEvent _levelStartEvent;
        [SerializeField] private LevelCompleteEvent _levelCompleteEvent;
        [SerializeField] private LevelFailEvent _levelFailEvent;

        private readonly Dictionary<EventChannelType, string> _eventNames = new();

        private void Awake()
        {
            if (_config == null) return;

            foreach (var tracked in _config.TrackedEvents)
            {
                if (tracked != null)
                {
                    _eventNames[tracked.ChannelType] = tracked.EventName;
                }
            }
        }

        private void OnEnable()
        {
            if (_levelStartEvent != null)
                _levelStartEvent.Subscribe(OnLevelStart);
            if (_levelCompleteEvent != null)
                _levelCompleteEvent.Subscribe(OnLevelComplete);
            if (_levelFailEvent != null)
                _levelFailEvent.Subscribe(OnLevelFail);
        }

        private void OnDisable()
        {
            if (_levelStartEvent != null)
                _levelStartEvent.Unsubscribe(OnLevelStart);
            if (_levelCompleteEvent != null)
                _levelCompleteEvent.Unsubscribe(OnLevelComplete);
            if (_levelFailEvent != null)
                _levelFailEvent.Unsubscribe(OnLevelFail);
        }

        private void OnLevelStart(LevelEventData data)
        {
            string eventName = GetEventName(EventChannelType.LevelStart);
            LogEvent(eventName, new Dictionary<string, object>
            {
                { "level_index", data.LevelIndex },
                { "level_type", data.LevelType },
                { "shields_remaining", data.ShieldsRemaining }
            });
        }

        private void OnLevelComplete(LevelCompleteData data)
        {
            string eventName = GetEventName(EventChannelType.LevelComplete);
            LogEvent(eventName, new Dictionary<string, object>
            {
                { "level_index", data.LevelIndex },
                { "level_type", data.LevelType },
                { "stars_earned", data.StarsEarned },
                { "shields_remaining", data.ShieldsRemaining },
                { "is_new_best", data.IsNewBest }
            });
        }

        private void OnLevelFail(LevelFailData data)
        {
            string eventName = GetEventName(EventChannelType.LevelFail);
            LogEvent(eventName, new Dictionary<string, object>
            {
                { "level_index", data.LevelIndex },
                { "level_type", data.LevelType },
                { "fail_reason", data.FailReason },
                { "shields_remaining", data.ShieldsRemaining }
            });
        }

        private string GetEventName(EventChannelType channelType)
        {
            return _eventNames.TryGetValue(channelType, out string name) ? name : channelType.ToString();
        }

        private void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            string paramStr = "";
            foreach (var kvp in parameters)
            {
                paramStr += $"  {kvp.Key}: {kvp.Value}\n";
            }
            Debug.Log($"[Analytics] {eventName}\n{paramStr}");
        }
    }
}
