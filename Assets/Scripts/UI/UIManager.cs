using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GravityRocket.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _menuPanel;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private GameObject _shieldDepletedPanel;
        [SerializeField] private GameObject _levelCompletePanel;
        [SerializeField] private GameObject _victoryPanel;

        [Header("HUD")]
        [SerializeField] private TMP_Text _sectorText;
        [SerializeField] private TMP_Text _starsText;
        [SerializeField] private TMP_Text _shieldText;
        [SerializeField] private TMP_Text _instructionText;
        [SerializeField] private GameObject _hudPanel;

        [Header("Level Complete")]
        [SerializeField] private TMP_Text _levelStarsDisplay;
        [SerializeField] private TMP_Text _totalStarsDisplay;

        [Header("Victory")]
        [SerializeField] private TMP_Text _finalStarsDisplay;

        [Header("Buttons")]
        [SerializeField] private Button _startBtn;
        [SerializeField] private Button _retryBtn;
        [SerializeField] private Button _refillBtn;
        [SerializeField] private Button _retreatBtn;
        [SerializeField] private Button _replayBtn;
        [SerializeField] private Button _nextBtn;
        [SerializeField] private Button _restartBtn;

        [Header("Audio Settings")]
        [SerializeField] private Button _muteToggleBtn;
        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private TMP_Text _muteToggleText;

        [Header("Safe Area")]
        [SerializeField] private RectTransform _safeAreaRect;

        private System.Action _onStart;
        private System.Action _onRetry;
        private System.Action _onRefill;
        private System.Action _onRetreat;
        private System.Action _onReplay;
        private System.Action _onNext;
        private System.Action _onRestart;

        private void Awake()
        {
            ApplySafeArea();
        }

        public void Initialize(
            System.Action onStart,
            System.Action onRetry,
            System.Action onRefill,
            System.Action onRetreat,
            System.Action onReplay,
            System.Action onNext,
            System.Action onRestart)
        {
            _onStart = onStart;
            _onRetry = onRetry;
            _onRefill = onRefill;
            _onRetreat = onRetreat;
            _onReplay = onReplay;
            _onNext = onNext;
            _onRestart = onRestart;

            _startBtn.onClick.AddListener(() => _onStart?.Invoke());
            _retryBtn.onClick.AddListener(() => _onRetry?.Invoke());
            _refillBtn.onClick.AddListener(() => _onRefill?.Invoke());
            _retreatBtn.onClick.AddListener(() => _onRetreat?.Invoke());
            _replayBtn.onClick.AddListener(() => _onReplay?.Invoke());
            _nextBtn.onClick.AddListener(() => _onNext?.Invoke());
            _restartBtn.onClick.AddListener(() => _onRestart?.Invoke());

            if (_muteToggleBtn != null)
                _muteToggleBtn.onClick.AddListener(OnMuteToggle);
            if (_volumeSlider != null)
            {
                _volumeSlider.minValue = 0;
                _volumeSlider.maxValue = 100;
                _volumeSlider.wholeNumbers = true;
                _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

                if (Audio.AudioManager.Instance != null)
                {
                    _volumeSlider.value = Audio.AudioManager.Instance.Volume;
                    UpdateMuteButtonText();
                }
            }
        }

        private void OnMuteToggle()
        {
            if (Audio.AudioManager.Instance == null) return;
            Audio.AudioManager.Instance.ToggleMute();
            UpdateMuteButtonText();
        }

        private void OnVolumeChanged(float value)
        {
            if (Audio.AudioManager.Instance == null) return;
            Audio.AudioManager.Instance.SetVolume((int)value);
        }

        private void UpdateMuteButtonText()
        {
            if (_muteToggleText == null || Audio.AudioManager.Instance == null) return;
            _muteToggleText.text = Audio.AudioManager.Instance.IsMuted ? "SOUND: OFF" : "SOUND: ON";
        }

        public void ShowMenu()
        {
            HideAllPanels();
            _menuPanel.SetActive(true);
            _hudPanel.SetActive(false);
        }

        public void ShowAiming(int level, int totalStars, int shields, int maxShields)
        {
            HideAllPanels();
            _hudPanel.SetActive(true);
            UpdateHUD(level, totalStars, shields, maxShields);
            ShowInstruction("DRAG TO PLOT TRAJECTORY");
        }

        public void ShowGameOver()
        {
            _gameOverPanel.SetActive(true);
            HideInstruction();
        }

        public void ShowShieldDepleted()
        {
            _shieldDepletedPanel.SetActive(true);
            HideInstruction();
        }

        public void ShowLevelComplete(int starsEarned, int totalStars)
        {
            HideInstruction();

            string starText = "";
            for (int i = 0; i < 3; i++)
                starText += (i < starsEarned) ? "*" : ".";
            _levelStarsDisplay.text = starText;
            _totalStarsDisplay.text = $"Total Stars: {totalStars} / 150";

            _levelCompletePanel.SetActive(true);
        }

        public void ShowVictory(int totalStars)
        {
            HideAllPanels();
            _hudPanel.SetActive(false);
            _finalStarsDisplay.text = $"Total Stars: {totalStars} / 150";
            _victoryPanel.SetActive(true);
        }

        public void UpdateHUD(int level, int totalStars, int shields, int maxShields)
        {
            _sectorText.text = $"Sector {level + 1}/50";
            _starsText.text = $"* {totalStars}";

            string shieldStr = "";
            for (int i = 0; i < maxShields; i++)
                shieldStr += (i < shields) ? "|" : ".";
            _shieldText.text = $"SHIELD: {shieldStr}";

            if (shields > 2)
                _shieldText.color = Color.white;
            else if (shields > 0)
                _shieldText.color = new Color(0.67f, 0.67f, 0.67f);
            else
                _shieldText.color = new Color(0.33f, 0.33f, 0.33f);
        }

        public void HideInstruction()
        {
            if (_instructionText != null)
                _instructionText.gameObject.SetActive(false);
        }

        private void ShowInstruction(string text)
        {
            if (_instructionText != null)
            {
                _instructionText.text = text;
                _instructionText.gameObject.SetActive(true);
            }
        }

        public void HideAllPanels()
        {
            _menuPanel.SetActive(false);
            _gameOverPanel.SetActive(false);
            _shieldDepletedPanel.SetActive(false);
            _levelCompletePanel.SetActive(false);
            _victoryPanel.SetActive(false);
        }

        private void ApplySafeArea()
        {
            if (_safeAreaRect == null) return;

            var safeArea = Screen.safeArea;
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _safeAreaRect.anchorMin = anchorMin;
            _safeAreaRect.anchorMax = anchorMax;
        }
    }
}
