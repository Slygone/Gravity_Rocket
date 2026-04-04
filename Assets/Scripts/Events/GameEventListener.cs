using UnityEngine;
using UnityEngine.Events;

namespace GravityRocket.Events
{
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent _event;
        [SerializeField] private UnityEvent _response;

        private void OnEnable()
        {
            if (_event != null)
                _event.Subscribe(OnEventRaised);
        }

        private void OnDisable()
        {
            if (_event != null)
                _event.Unsubscribe(OnEventRaised);
        }

        private void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
}
