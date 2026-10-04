using UnityEngine;

/// <summary>
/// Sits on the buggy and tells the AudioManager how hard the engine is working.
/// The engine runs only while the run is being driven: it dies with the fuel, a crash, or the finish.
/// Nitro makes it scream: a whoosh when it kicks in, and a higher pitch while it burns.
/// </summary>
public class EngineAudio : MonoBehaviour
{
    [SerializeField] private VehicleController _vehicle;
    [Tooltip("Revs while the throttle is held but the wheels are not yet spinning, so pressing gas is always heard.")]
    [SerializeField, Range(0f, 1f)] private float _throttleRevs = 0.35f;
    [Tooltip("Played once when the nitro kicks in.")]
    [SerializeField] private AudioClip _nitroStartSound;

    private bool _wasNitroActive;

    private void Update()
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null)
        {
            return;
        }

        GameManager game = GameManager.Instance;
        bool running = game != null && game.State == RunState.Playing;

        float revs = _vehicle.WheelSpinNormalized;
        if (_vehicle.ThrottleHeld)
        {
            revs = Mathf.Max(revs, _throttleRevs);
        }

        bool nitro = _vehicle.IsNitroActive;
        if (nitro && !_wasNitroActive)
        {
            audio.PlaySfx(_nitroStartSound);
        }

        _wasNitroActive = nitro;
        audio.SetEngine(running, revs, nitro);
    }

    private void OnDisable()
    {
        // The AudioManager outlives this buggy: going back to the menu must not leave the engine running.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetEngine(false, 0f);
        }
    }
}
