using UnityEngine;

/// <summary>
/// The crunch of the tyres on the dirt. Louder and higher the faster the buggy rolls,
/// silent while it is in the air.
/// </summary>
public class WheelRollAudio : MonoBehaviour
{
    [SerializeField] private VehicleController _vehicle;
    [SerializeField] private CircleCollider2D[] _wheels;
    [SerializeField] private LayerMask _groundLayer;
    [Tooltip("Speed (units per second) at which the roll sound is at full volume.")]
    [SerializeField, Min(0.1f)] private float _fullVolumeSpeed = 12f;

    private void Update()
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null)
        {
            return;
        }

        float intensity = IsAnyWheelGrounded() ? _vehicle.Speed / _fullVolumeSpeed : 0f;
        audio.SetRoll(intensity);
    }

    private void OnDisable()
    {
        // The AudioManager outlives this buggy on a restart, so leave it silent.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetRoll(0f);
        }
    }

    private bool IsAnyWheelGrounded()
    {
        foreach (CircleCollider2D wheel in _wheels)
        {
            if (wheel.IsTouchingLayers(_groundLayer))
            {
                return true;
            }
        }

        return false;
    }
}
