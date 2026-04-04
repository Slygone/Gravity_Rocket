using System;
using System.Collections.Generic;
using UnityEngine;

namespace GravityRocket.Events
{
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "Gravity Rocket/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<Action> _listeners = new();

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i]?.Invoke();
            }
        }

        public void Subscribe(Action listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void Unsubscribe(Action listener)
        {
            _listeners.Remove(listener);
        }
    }

    public abstract class GameEvent<T> : ScriptableObject
    {
        private readonly List<Action<T>> _listeners = new();

        public void Raise(T data)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i]?.Invoke(data);
            }
        }

        public void Subscribe(Action<T> listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void Unsubscribe(Action<T> listener)
        {
            _listeners.Remove(listener);
        }
    }
}
