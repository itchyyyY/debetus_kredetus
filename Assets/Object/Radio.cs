using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Collider))]
public class Radio : MonoBehaviour, IInteractable, ITurnOffable
{
    [Header("Список треков")]
    [Tooltip("Перетащите сюда аудиофайлы (mp3, wav, ogg)")]
    [SerializeField] private AudioClip[] _tracks;

    [Header("Настройки воспроизведения")]
    [Range(0f, 1f)]
    [SerializeField] private float _volume = 0.8f;
    [Tooltip("Автоматически включать следующий трек после окончания предыдущего")]
    [SerializeField] private bool _autoPlayNextTrack = true;
    [Tooltip("Зацикливать один и тот же трек вместо переключения")]
    [SerializeField] private bool _loopSingleTrack = false;

    [Header("Подсказки для экрана")]
    [SerializeField] private string _turnOnPrompt = "Включить радио (E)";
    [SerializeField] private string _emptyPrompt = "В радио нет треков";

    private AudioSource _audioSource;
    private int _currentTrackIndex = -1; // -1 = выключено

    public bool IsPlaying => _currentTrackIndex >= 0;

    public string PromptMessage
    {
        get
        {
            if (_tracks == null || _tracks.Length == 0)
                return _emptyPrompt;

            if (!IsPlaying)
                return _turnOnPrompt;

            int currentNum = _currentTrackIndex + 1;
            return $"След. трек [{currentNum}/{_tracks.Length}] (E) | Выключить (R)";
        }
    }

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = _loopSingleTrack;
        _audioSource.spatialBlend = 1.0f; // Полный 3D-звук в пространстве
        _audioSource.volume = _volume;
    }

    private void Update()
    {
        // Автоматический переход к следующему треку, когда песня доиграла
        if (IsPlaying && !_audioSource.isPlaying && _autoPlayNextTrack && !_loopSingleTrack)
        {
            if (_tracks != null && _tracks.Length > 0)
            {
                _currentTrackIndex = (_currentTrackIndex + 1) % _tracks.Length;
                PlayCurrentTrack();
            }
        }
    }

    /// <summary>
    /// Срабатывает при нажатии E (включение или переключение трека)
    /// </summary>
    public void Interact(PlayerInteraction player)
    {
        if (_tracks == null || _tracks.Length == 0)
        {
            Debug.LogWarning("[Radio] Список треков пуст! Добавьте AudioClip в инспекторе.");
            return;
        }

        if (!IsPlaying)
        {
            // Включаем с первого трека
            _currentTrackIndex = 0;
        }
        else
        {
            // Переключаем на следующий трек по кругу
            _currentTrackIndex = (_currentTrackIndex + 1) % _tracks.Length;
        }

        PlayCurrentTrack();
    }

    /// <summary>
    /// Срабатывает при нажатии R (выключение радио)
    /// </summary>
    public void TurnOff()
    {
        if (!IsPlaying) return;

        _currentTrackIndex = -1;
        _audioSource.Stop();
        _audioSource.clip = null;
        Debug.Log("[Radio] Радио выключено");
    }

    private void PlayCurrentTrack()
    {
        if (_currentTrackIndex < 0 || _currentTrackIndex >= _tracks.Length) return;

        AudioClip clipToPlay = _tracks[_currentTrackIndex];
        if (clipToPlay == null)
        {
            Debug.LogWarning($"[Radio] Трек #{_currentTrackIndex + 1} не назначен!");
            return;
        }

        _audioSource.clip = clipToPlay;
        _audioSource.loop = _loopSingleTrack;
        _audioSource.volume = _volume;
        _audioSource.Play();

        Debug.Log($"[Radio] Играет трек [{_currentTrackIndex + 1}/{_tracks.Length}]: {clipToPlay.name}");
    }
}
