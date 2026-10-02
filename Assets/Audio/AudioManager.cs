using UnityEngine;

/// <summary>
/// Plays the engine loop and one-shot sound effects (GDD §6, §7).
/// A persistent singleton: it survives the scene reload on every retry, so there is
/// exactly one engine source for the whole session instead of a new one per attempt.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sources")]
    [Tooltip("Looping source with the engine clip. Play On Awake off.")]
    [SerializeField] private AudioSource _engineSource;
    [Tooltip("Source for one-shot effects. No clip needed.")]
    [SerializeField] private AudioSource _sfxSource;

    [Header("Engine pitch")]
    [SerializeField, Min(0.1f)] private float _idlePitch = 0.8f;
    [SerializeField, Min(0.1f)] private float _maxPitch = 2f;
    [Tooltip("How quickly the pitch follows the revs. Higher = snappier.")]
    [SerializeField, Min(0.1f)] private float _pitchResponse = 8f;

    private bool _engineRunning;
    private float _targetPitch;

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
        _targetPitch = _idlePitch;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
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
        float blend = 1f - Mathf.Exp(-_pitchResponse * Time.deltaTime);
        _engineSource.pitch = Mathf.Lerp(_engineSource.pitch, _targetPitch, blend);
    }

    /// <summary>Called every frame by the vehicle. revs is 0 at idle and 1 at full wheel speed.</summary>
    public void SetEngine(bool running, float revs)
    {
        _engineRunning = running;
        _targetPitch = Mathf.Lerp(_idlePitch, _maxPitch, Mathf.Clamp01(revs));
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip != null)
        {
            _sfxSource.PlayOneShot(clip);
        }
    }
}
