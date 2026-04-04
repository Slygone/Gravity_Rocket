using UnityEngine;

namespace GravityRocket.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioClip _backgroundMusic;

        private AudioSource _audioSource;
        private bool _isMuted;
        private int _volume = 80;

        private const string PrefKeyEnabled = "MusicEnabled";
        private const string PrefKeyVolume = "MusicVolume";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;

            LoadPrefs();
            ApplySettings();

            if (_backgroundMusic != null)
            {
                _audioSource.clip = _backgroundMusic;
                _audioSource.Play();
            }
        }

        public void SetVolume(int volume)
        {
            _volume = Mathf.Clamp(volume, 0, 100);
            ApplySettings();
            SavePrefs();
        }

        public void SetMuted(bool muted)
        {
            _isMuted = muted;
            ApplySettings();
            SavePrefs();
        }

        public void ToggleMute()
        {
            SetMuted(!_isMuted);
        }

        public bool IsMuted => _isMuted;
        public int Volume => _volume;

        private void ApplySettings()
        {
            if (_audioSource == null) return;
            _audioSource.volume = _isMuted ? 0f : _volume / 100f;
        }

        private void LoadPrefs()
        {
            _isMuted = PlayerPrefs.GetInt(PrefKeyEnabled, 1) == 0;
            _volume = PlayerPrefs.GetInt(PrefKeyVolume, 80);
        }

        private void SavePrefs()
        {
            PlayerPrefs.SetInt(PrefKeyEnabled, _isMuted ? 0 : 1);
            PlayerPrefs.SetInt(PrefKeyVolume, _volume);
            PlayerPrefs.Save();
        }
    }
}
