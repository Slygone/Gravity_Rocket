using UnityEngine;
using GravityRocket.Gameplay;
using GravityRocket.Events;
using GravityRocket.UI;
using GravityRocket.VFX;

namespace GravityRocket.Core
{
    public enum GameState
    {
        Menu,
        Aiming,
        Flying,
        LevelComplete,
        GameOver,
        ShieldDepleted,
        Victory
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private Rocket _rocket;
        [SerializeField] private AimController _aimController;
        [SerializeField] private ObjectPool _planetPool;
        [SerializeField] private GoalZone _goalZone;
        [SerializeField] private GoalZoneRenderer _goalZoneRenderer;
        [SerializeField] private ShockwavePulse _shockwavePulse;
        [SerializeField] private Camera _mainCamera;

        [Header("Events")]
        [SerializeField] private LevelStartEvent _levelStartEvent;
        [SerializeField] private LevelCompleteEvent _levelCompleteEvent;
        [SerializeField] private LevelFailEvent _levelFailEvent;

        [Header("Settings")]
        [SerializeField] private int _maxShields = 5;

        private GameState _state = GameState.Menu;
        private LevelData[] _levels;
        private LevelData _currentLevelData;
        private int _currentLevel;
        private int _currentShields;
        private int[] _levelStars;
        private GravityField[] _activePlanets;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _levels = LevelGenerator.GenerateAllLevels();
            _levelStars = new int[LevelGenerator.TotalLevels];
            _currentShields = _maxShields;

            LoadProgress();
            SetupCamera();
        }

        private void Start()
        {
            _uiManager.Initialize(
                onStart: OnStartPressed,
                onRetry: OnRetryPressed,
                onRefill: OnRefillPressed,
                onRetreat: OnRetreatPressed,
                onReplay: OnReplayPressed,
                onNext: OnNextPressed,
                onRestart: OnRestartPressed
            );

            _uiManager.ShowMenu();
        }

        private void SetupCamera()
        {
            if (_mainCamera == null) return;

            _mainCamera.orthographic = true;
            _mainCamera.orthographicSize = LevelGenerator.WorldHeight / 2f;
            _mainCamera.transform.position = new Vector3(
                LevelGenerator.WorldWidth / 2f,
                LevelGenerator.WorldHeight / 2f,
                -10f
            );

            float aspect = (float)Screen.width / Screen.height;
            float requiredOrthoSize = LevelGenerator.WorldWidth / (2f * aspect);
            if (requiredOrthoSize > _mainCamera.orthographicSize)
            {
                _mainCamera.orthographicSize = requiredOrthoSize;
                _mainCamera.transform.position = new Vector3(
                    LevelGenerator.WorldWidth / 2f,
                    _mainCamera.orthographicSize,
                    -10f
                );
            }
        }

        private void LoadLevel(int index)
        {
            _currentLevel = index;
            _currentLevelData = _levels[_currentLevel];

            if (_planetPool != null)
                _planetPool.ReturnAll();

            _activePlanets = new GravityField[_currentLevelData.Planets.Length];
            for (int i = 0; i < _currentLevelData.Planets.Length; i++)
            {
                var config = _currentLevelData.Planets[i];
                var planetObj = _planetPool.Get();
                planetObj.transform.position = new Vector3(config.Position.x, config.Position.y, 0f);

                var gravityField = planetObj.GetComponent<GravityField>();
                if (gravityField != null)
                {
                    gravityField.Configure(config.Mass, config.Radius);
                    gravityField.SetRocket(_rocket.Rb);
                }

                _activePlanets[i] = gravityField;
            }

            if (_goalZone != null)
            {
                _goalZone.gameObject.SetActive(true);
                _goalZone.Configure(_currentLevelData.GoalPosition, _currentLevelData.GoalWidth, OnGoalReached);
            }
            if (_goalZoneRenderer != null)
            {
                _goalZoneRenderer.Configure(_currentLevelData.GoalPosition, _currentLevelData.GoalWidth);
            }

            _rocket.ResetToPosition(
                _currentLevelData.StartPosition,
                onOutOfBounds: OnRocketOutOfBounds,
                onPlanetCrash: OnRocketCrash
            );

            _aimController.EnableAiming(OnLaunch);

            _state = GameState.Aiming;

            int totalStars = GetTotalStars();
            _uiManager.ShowAiming(_currentLevel, totalStars, _currentShields, _maxShields);

            if (_levelStartEvent != null)
            {
                _levelStartEvent.Raise(new LevelEventData
                {
                    LevelIndex = _currentLevel,
                    LevelType = _currentLevelData.LayoutType,
                    ShieldsRemaining = _currentShields
                });
            }
        }

        private void OnLaunch(Vector2 direction)
        {
            _state = GameState.Flying;
            _aimController.DisableAiming();
            _rocket.Launch(direction);
            _uiManager.HideInstruction();
        }

        private void OnGoalReached(float hitRatio)
        {
            if (_state != GameState.Flying) return;

            _state = GameState.LevelComplete;
            _aimController.DisableAiming();

            int starsEarned;
            if (hitRatio > 0.4f && hitRatio < 0.6f)
                starsEarned = 3;
            else if (hitRatio > 0.2f && hitRatio < 0.8f)
                starsEarned = 2;
            else
                starsEarned = 1;

            bool isNewBest = starsEarned > _levelStars[_currentLevel];
            _levelStars[_currentLevel] = Mathf.Max(_levelStars[_currentLevel], starsEarned);

            _currentShields = Mathf.Min(_currentShields + 1, _maxShields);

            int totalStars = GetTotalStars();
            _uiManager.UpdateHUD(_currentLevel, totalStars, _currentShields, _maxShields);

            float goalCenterY = _currentLevelData.GoalPosition.y + 0.375f;
            _rocket.StartWarp(new Vector2(_rocket.transform.position.x, goalCenterY));

            if (_shockwavePulse != null)
            {
                _shockwavePulse.TriggerPulse(new Vector2(_rocket.transform.position.x, goalCenterY));
            }

            SaveProgress();

            Invoke(nameof(ShowLevelCompleteUI), 1.5f);

            _pendingStars = starsEarned;
            _pendingTotalStars = totalStars;
            _pendingIsNewBest = isNewBest;

            if (_levelCompleteEvent != null)
            {
                _levelCompleteEvent.Raise(new LevelCompleteData
                {
                    LevelIndex = _currentLevel,
                    LevelType = _currentLevelData.LayoutType,
                    StarsEarned = starsEarned,
                    ShieldsRemaining = _currentShields,
                    IsNewBest = isNewBest
                });
            }
        }

        private int _pendingStars;
        private int _pendingTotalStars;
        private bool _pendingIsNewBest;

        private void ShowLevelCompleteUI()
        {
            if (_currentLevel >= LevelGenerator.TotalLevels - 1)
            {
                _uiManager.ShowVictory(_pendingTotalStars);
                _state = GameState.Victory;
            }
            else
            {
                _uiManager.ShowLevelComplete(_pendingStars, _pendingTotalStars);
            }
        }

        private void OnRocketCrash()
        {
            HandleFailure("crash", true);
        }

        private void OnRocketOutOfBounds(string reason)
        {
            HandleFailure(reason, false);
        }

        private void HandleFailure(string reason, bool crashed)
        {
            if (_state != GameState.Flying) return;

            _state = GameState.GameOver;
            _aimController.DisableAiming();

            // Reset chromatic aberration
            VFX.ChromaticAberrationFeature.SetOffset(Vector2.zero);

            if (crashed)
                _rocket.Crash();

            _currentShields--;
            _uiManager.UpdateHUD(_currentLevel, GetTotalStars(), _currentShields, _maxShields);

            SaveProgress();

            float delay = crashed ? 0.8f : 0.2f;

            if (_currentShields > 0)
            {
                Invoke(nameof(ShowGameOverUI), delay);
            }
            else
            {
                _state = GameState.ShieldDepleted;
                Invoke(nameof(ShowShieldDepletedUI), delay);
            }

            if (_levelFailEvent != null)
            {
                _levelFailEvent.Raise(new LevelFailData
                {
                    LevelIndex = _currentLevel,
                    LevelType = _currentLevelData.LayoutType,
                    FailReason = reason,
                    ShieldsRemaining = _currentShields
                });
            }
        }

        private void ShowGameOverUI() => _uiManager.ShowGameOver();
        private void ShowShieldDepletedUI() => _uiManager.ShowShieldDepleted();

        private void OnStartPressed()
        {
            _currentShields = _maxShields;
            LoadLevel(0);
        }

        private void OnRetryPressed() => LoadLevel(_currentLevel);
        private void OnReplayPressed() => LoadLevel(_currentLevel);
        private void OnNextPressed() => LoadLevel(_currentLevel + 1);

        private void OnRefillPressed()
        {
            _currentShields = _maxShields;
            LoadLevel(_currentLevel);
        }

        private void OnRetreatPressed()
        {
            _currentShields = _maxShields;
            LoadLevel(Mathf.Max(0, _currentLevel - 1));
        }

        private void OnRestartPressed()
        {
            _levelStars = new int[LevelGenerator.TotalLevels];
            _currentShields = _maxShields;
            SaveProgress();
            LoadLevel(0);
        }

        private int GetTotalStars()
        {
            int total = 0;
            foreach (int s in _levelStars) total += s;
            return total;
        }

        private void SaveProgress()
        {
            for (int i = 0; i < _levelStars.Length; i++)
                PlayerPrefs.SetInt($"Stars_{i}", _levelStars[i]);
            PlayerPrefs.SetInt("CurrentLevel", _currentLevel);
            PlayerPrefs.SetInt("Shields", _currentShields);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            for (int i = 0; i < _levelStars.Length; i++)
                _levelStars[i] = PlayerPrefs.GetInt($"Stars_{i}", 0);
            _currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
            _currentShields = PlayerPrefs.GetInt("Shields", _maxShields);
        }
    }
}
