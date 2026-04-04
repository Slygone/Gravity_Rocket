using UnityEngine;
using UnityEngine.InputSystem;

namespace GravityRocket.Gameplay
{
    public class AimController : MonoBehaviour
    {
        [SerializeField] private LineRenderer _aimLine;
        [SerializeField] private LineRenderer _launchPadCircle;
        [SerializeField] private float _aimLineMaxLength = 5f;

        private Camera _mainCamera;
        private bool _isDragging;
        private Vector2 _dragWorldPos;
        private bool _aimingEnabled;

        private Rocket _rocket;

        private System.Action<Vector2> _onLaunch;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _rocket = GetComponent<Rocket>();
        }

        public void EnableAiming(System.Action<Vector2> onLaunch)
        {
            _onLaunch = onLaunch;
            _aimingEnabled = true;
            _isDragging = false;

            if (_aimLine != null)
            {
                _aimLine.positionCount = 0;
                _aimLine.enabled = false;
            }

            ShowLaunchPad(transform.position);
        }

        public void DisableAiming()
        {
            _aimingEnabled = false;
            _isDragging = false;

            if (_aimLine != null)
                _aimLine.enabled = false;

            HideLaunchPad();
        }

        private void Update()
        {
            if (!_aimingEnabled) return;

            bool pointerDown = Mouse.current != null && Mouse.current.leftButton.isPressed;
            Vector2 pointerScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pointerDown = true;
                pointerScreenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }

            if (pointerDown)
            {
                _isDragging = true;
                _dragWorldPos = _mainCamera.ScreenToWorldPoint(pointerScreenPos);
                UpdateAimLine();
            }
            else if (_isDragging)
            {
                _isDragging = false;
                if (_aimLine != null) _aimLine.enabled = false;

                Vector2 rocketPos = transform.position;
                Vector2 direction = (_dragWorldPos - rocketPos).normalized;
                _onLaunch?.Invoke(direction);
            }
        }

        private void UpdateAimLine()
        {
            if (_aimLine == null) return;

            Vector2 rocketPos = transform.position;
            Vector2 direction = _dragWorldPos - rocketPos;

            if (direction.magnitude > _aimLineMaxLength)
            {
                direction = direction.normalized * _aimLineMaxLength;
            }

            _aimLine.enabled = true;
            _aimLine.positionCount = 2;
            _aimLine.SetPosition(0, new Vector3(rocketPos.x, rocketPos.y, 0));
            _aimLine.SetPosition(1, new Vector3(rocketPos.x + direction.x, rocketPos.y + direction.y, 0));

            _aimLine.startWidth = 0.03f;
            _aimLine.endWidth = 0.03f;
            _aimLine.startColor = new Color(1f, 1f, 1f, 0.4f);
            _aimLine.endColor = new Color(1f, 1f, 1f, 0.4f);
        }

        public void ShowLaunchPad(Vector2 position)
        {
            if (_launchPadCircle == null) return;

            int segments = 32;
            float radius = 0.5f;
            _launchPadCircle.positionCount = segments + 1;
            _launchPadCircle.loop = false;
            _launchPadCircle.startWidth = 0.02f;
            _launchPadCircle.endWidth = 0.02f;
            _launchPadCircle.startColor = new Color(1f, 1f, 1f, 0.3f);
            _launchPadCircle.endColor = new Color(1f, 1f, 1f, 0.3f);
            _launchPadCircle.enabled = true;

            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float x = position.x + Mathf.Cos(angle) * radius;
                float y = position.y + Mathf.Sin(angle) * radius;
                _launchPadCircle.SetPosition(i, new Vector3(x, y, 0f));
            }
        }

        public void HideLaunchPad()
        {
            if (_launchPadCircle != null)
                _launchPadCircle.enabled = false;
        }
    }
}
