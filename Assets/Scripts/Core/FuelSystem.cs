using System;
using UnityEngine;

/// <summary>
/// Drains and refills the vehicle's fuel, and raises the event that starts the coast-out (GDD §3).
/// Fuel is only ever granted by a can - there is no regeneration and no reserve.
/// </summary>
public class FuelSystem : MonoBehaviour
{
    [SerializeField] private CourseConfig _courseConfig;
    [SerializeField] private VehicleController _vehicle;

    [Tooltip("Shown for debugging only - set from the CourseConfig at the start of the run.")]
    [SerializeField] private float _currentFuel;

    public float CurrentFuel => _currentFuel;
    public float Normalized => _currentFuel / _courseConfig.FuelCapacity;
    public bool IsEmpty => _currentFuel <= 0f;

    /// <summary>Raised whenever the amount changes, with the new amount as 0..1. The HUD gauge listens.</summary>
    public event Action<float> Changed;

    /// <summary>Raised once, on the frame the tank runs dry.</summary>
    public event Action Emptied;

    private void Awake()
    {
        _currentFuel = _courseConfig.FuelCapacity;
    }

    private void Update()
    {
        if (IsEmpty || GameManager.Instance.State != RunState.Playing)
        {
            return;
        }

        float drainPerSecond = _courseConfig.FuelDrainIdle;
        if (_vehicle.ThrottleHeld)
        {
            drainPerSecond += _courseConfig.FuelDrainThrottle;
        }

        SetFuel(_currentFuel - drainPerSecond * Time.deltaTime);

        if (IsEmpty)
        {
            Emptied?.Invoke();
        }
    }

    /// <summary>
    /// Adds one can's worth of fuel and returns true.
    /// Returns false once the tank has run dry: the coast-out is final.
    /// </summary>
    public bool TryAddCan()
    {
        if (IsEmpty || GameManager.Instance.State != RunState.Playing)
        {
            return false;
        }

        SetFuel(_currentFuel + _courseConfig.FuelPerCan);
        return true;
    }

    private void SetFuel(float amount)
    {
        _currentFuel = Mathf.Clamp(amount, 0f, _courseConfig.FuelCapacity);
        Changed?.Invoke(Normalized);
    }
}
