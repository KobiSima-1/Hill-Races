using TMPro;
using UnityEngine;

/// <summary>
/// A needle gauge on the HUD that shows how fast the buggy is going (GDD §6).
/// The needle eases toward the real speed so bumps don't make it jitter.
/// </summary>
public class Speedometer : MonoBehaviour
{
    [SerializeField] private VehicleController _vehicle;
    [Tooltip("The needle image. Its pivot must be the centre of the dial.")]
    [SerializeField] private RectTransform _needle;
    [Tooltip("Optional digital readout under the needle.")]
    [SerializeField] private TMP_Text _readout;

    [Header("Scale")]
    [Tooltip("Game units per second times this = the km/h on the dial.")]
    [SerializeField, Min(0f)] private float _kmhPerUnit = 6f;
    [Tooltip("The highest number printed on the dial.")]
    [SerializeField, Min(1f)] private float _maxKmh = 120f;
    [Tooltip("Needle angle at 0 km/h (degrees, counter-clockwise from straight up).")]
    [SerializeField] private float _zeroAngle = 120f;
    [Tooltip("Needle angle at the highest number.")]
    [SerializeField] private float _maxAngle = -120f;
    [Tooltip("How quickly the needle follows the speed. Higher = snappier.")]
    [SerializeField, Min(0.1f)] private float _response = 8f;

    private float _shownKmh;

    private void Update()
    {
        float targetKmh = _vehicle.Speed * _kmhPerUnit;
        float blend = 1f - Mathf.Exp(-_response * Time.deltaTime);
        _shownKmh = Mathf.Lerp(_shownKmh, targetKmh, blend);

        float angle = Mathf.Lerp(_zeroAngle, _maxAngle, Mathf.Clamp01(_shownKmh / _maxKmh));
        _needle.localEulerAngles = new Vector3(0f, 0f, angle);

        if (_readout != null)
        {
            _readout.text = Mathf.RoundToInt(_shownKmh).ToString();
        }
    }
}
