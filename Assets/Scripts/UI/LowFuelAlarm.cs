using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Beeps, and blinks the fuel gauge, when the fuel runs low (GDD §3, §6).
/// The beeps speed up as the tank empties, so the player hears the pressure without looking.
/// </summary>
public class LowFuelAlarm : MonoBehaviour
{
    [SerializeField] private FuelSystem _fuel;
    [Tooltip("Optional. Leave empty for a silent, blink-only warning.")]
    [SerializeField] private AudioClip _beepSound;
    [Tooltip("The parts of the fuel gauge that blink: shown for the first half of every beep, hidden for the second.")]
    [SerializeField] private Graphic[] _blinkingGraphics;

    [Tooltip("Fuel fraction below which the alarm starts.")]
    [SerializeField, Range(0f, 1f)] private float _warningLevel = 0.2f;
    [Tooltip("Seconds between beeps when the alarm starts.")]
    [SerializeField, Min(0.1f)] private float _slowInterval = 1.2f;
    [Tooltip("Seconds between beeps when the tank is almost empty.")]
    [SerializeField, Min(0.1f)] private float _fastInterval = 0.4f;

    private float _timer;
    private float _beepInterval = 1f;

    private void Update()
    {
        if (!IsWarning())
        {
            _timer = 0f;
            SetGaugeVisible(true);
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            Beep();
        }

        SetGaugeVisible(_timer > _beepInterval * 0.5f);
    }

    private bool IsWarning()
    {
        GameManager game = GameManager.Instance;
        bool driving = game != null && game.State == RunState.Playing;
        return driving && !_fuel.IsEmpty && _fuel.Normalized <= _warningLevel;
    }

    private void Beep()
    {
        // Faster beeps the closer the tank is to empty.
        _beepInterval = Mathf.Lerp(_fastInterval, _slowInterval, _fuel.Normalized / _warningLevel);
        _timer = _beepInterval;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(_beepSound);
        }
    }

    private void SetGaugeVisible(bool visible)
    {
        foreach (Graphic graphic in _blinkingGraphics)
        {
            graphic.enabled = visible;
        }
    }
}
