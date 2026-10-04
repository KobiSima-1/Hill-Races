using UnityEngine;

/// <summary>
/// Plays the crash sound the moment the driver's head hits the ground.
/// </summary>
public class CrashAudio : MonoBehaviour
{
    [SerializeField] private CrashDetector _crashDetector;
    [SerializeField] private AudioClip _crashSound;

    private void OnEnable()
    {
        _crashDetector.Crashed += HandleCrashed;
    }

    private void OnDisable()
    {
        _crashDetector.Crashed -= HandleCrashed;
    }

    private void HandleCrashed()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(_crashSound);
        }
    }
}
