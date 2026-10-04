using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Plays the engine loop, the ground roll loop and one-shot sound effects.
/// A persistent singleton: it survives the scene reload on every retry, so there is
/// exactly one set of sources for the whole session instead of a new one per attempt.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [Tooltip("Looping source with the engine clip. Play On Awake off.")]
    [SerializeField] private AudioSource _engineSource;
    [Tooltip("Looping source with the ground roll clip. Play On Awake off.")]
    [SerializeField] private AudioSource _rollSource;
    [Tooltip("Source for one-shot effects. No clip needed.")]
    [SerializeField] private AudioSource _sfxSource;
    [Tooltip("Looping source with the music. Play On Awake on.")]
    [SerializeField] private AudioSource _musicSource;

    [Header("Engine pitch")]
    [SerializeField, Min(0.1f)] private float _idlePitch = 0.8f;
    [SerializeField, Min(0.1f)] private float _maxPitch = 2f;
    [Tooltip("How quickly the pitch follows the revs. Higher = snappier.")]
    [SerializeField, Min(0.1f)] private float _pitchResponse = 8f;
    [Tooltip("Added on top of the pitch while the nitro burns, so it screams above the normal maximum.")]
    [SerializeField, Min(0f)] private float _nitroPitchBoost = 0.5f;

    [Header("Ground roll")]
    [SerializeField, Range(0f, 1f)] private float _rollMaxVolume = 0.6f;
    [SerializeField, Min(0.1f)] private float _rollMinPitch = 0.8f;
    [SerializeField, Min(0.1f)] private float _rollMaxPitch = 1.3f;
    [Tooltip("How quickly the roll volume follows the ground contact. Higher = snappier.")]
    [SerializeField, Min(0.1f)] private float _rollResponse = 12f;

    private bool _engineRunning;
    private float _targetPitch;
    private float _rollIntensity;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // The course scene was reloaded and brought its own copy. The original stays.
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
        _musicSource.mute = !SaveService.LoadMusicEnabled();
        _targetPitch = _idlePitch;
        _rollSource.volume = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // This manager outlives the scene, and so would a long effect like the explosion.
        // A restart or a trip to the menu starts with silence.
        _sfxSource.Stop();
    }

    private void Update()
    {
        UpdateEngine();
        UpdateRoll();
    }

    private void UpdateEngine()
    {
        if (_engineRunning && !_engineSource.isPlaying)
        {
            _engineSource.Play();
        }
        else if (!_engineRunning && _engineSource.isPlaying)
        {
            _engineSource.Stop();
        }

        // Ease toward the target pitch so gear-like jumps in wheel speed don't sound like clicks.
        _engineSource.pitch = Mathf.Lerp(_engineSource.pitch, _targetPitch, Blend(_pitchResponse));
    }

    private void UpdateRoll()
    {
        // The roll loop always plays and is faded in and out, so touching down never clicks.
        if (!_rollSource.isPlaying)
        {
            _rollSource.Play();
        }

        float targetVolume = _rollIntensity * _rollMaxVolume;
        _rollSource.volume = Mathf.Lerp(_rollSource.volume, targetVolume, Blend(_rollResponse));
        _rollSource.pitch = Mathf.Lerp(_rollMinPitch, _rollMaxPitch, _rollIntensity);
    }

    private static float Blend(float response)
    {
        return 1f - Mathf.Exp(-response * Time.deltaTime);
    }

    /// <summary>Called every frame by the vehicle. revs is 0 at idle and 1 at full wheel speed.</summary>
    public void SetEngine(bool running, float revs, bool nitro = false)
    {
        _engineRunning = running;
        _targetPitch = Mathf.Lerp(_idlePitch, _maxPitch, Mathf.Clamp01(revs));
        if (nitro)
        {
            _targetPitch += _nitroPitchBoost;
        }
    }

    /// <summary>Called every frame by the vehicle. 0 = no wheel on the ground or standing still, 1 = full speed.</summary>
    public void SetRoll(float intensity)
    {
        _rollIntensity = Mathf.Clamp01(intensity);
    }

    public bool IsMusicEnabled => !_musicSource.mute;

    /// <summary>Turns the music on or off and remembers the choice for the next session.</summary>
    public void SetMusicEnabled(bool enabled)
    {
        // Mute rather than stop, so turning it back on continues the song instead of restarting it.
        _musicSource.mute = !enabled;
        SaveService.SaveMusicEnabled(enabled);
    }

    /// <summary>Plays a one-shot effect. volume scales this one clip only, from 0 to 1.</summary>
    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
        {
            _sfxSource.PlayOneShot(clip, volume);
        }
    }
}
