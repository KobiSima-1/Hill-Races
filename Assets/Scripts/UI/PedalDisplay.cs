using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The gas and brake pedals in the bottom corners of the HUD (GDD §6).
/// They press down while their input is held, like the pedals of the original game.
/// </summary>
public class PedalDisplay : MonoBehaviour
{
    [SerializeField] private VehicleController _vehicle;
    [SerializeField] private Image _gasPedal;
    [SerializeField] private Image _brakePedal;

    [Tooltip("Height of a pressed pedal. Set the pedal's pivot to its bottom edge so it presses down.")]
    [SerializeField, Range(0.5f, 1f)] private float _pressedScale = 0.88f;
    [SerializeField] private Color _pressedTint = new Color(0.7f, 0.7f, 0.7f);

    private void Update()
    {
        // Burning nitro drives the buggy on its own, so the gas pedal shows it.
        ShowPedal(_gasPedal, _vehicle.ThrottleHeld || _vehicle.IsNitroActive);
        ShowPedal(_brakePedal, _vehicle.BrakeHeld);
    }

    private void ShowPedal(Image pedal, bool pressed)
    {
        pedal.rectTransform.localScale = new Vector3(1f, pressed ? _pressedScale : 1f, 1f);
        pedal.color = pressed ? _pressedTint : Color.white;
    }
}
