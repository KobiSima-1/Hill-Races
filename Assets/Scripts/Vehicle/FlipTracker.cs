using System;
using UnityEngine;

/// <summary>
/// Counts full rotations while the buggy is in the air and rewards a clean landing with nitro (GDD §3).
/// A flip only counts if the buggy lands upright: a flip that ends on the roof earns nothing but a crash.
/// Several flips in one jump are a combo, and each extra flip adds a bonus on top.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class FlipTracker : MonoBehaviour
{
    [SerializeField] private VehicleController _vehicle;

    [Header("What counts as a flip")]
    [Tooltip("Degrees short of a full turn that still count, because take-off and landing slopes differ.")]
    [SerializeField, Range(0f, 90f)] private float _rotationTolerance = 50f;
    [Tooltip("The buggy must land within this many degrees of upright.")]
    [SerializeField, Range(0f, 90f)] private float _maxLandingTilt = 50f;

    [Header("Reward")]
    [Tooltip("Seconds of nitro for each flip.")]
    [SerializeField, Min(0f)] private float _nitroPerFlip = 1.5f;
    [Tooltip("Extra seconds for every flip after the first in the same jump.")]
    [SerializeField, Min(0f)] private float _comboBonus = 1f;

    /// <summary>Raised on a clean landing: number of flips, true for backflips, and the nitro earned.</summary>
    public event Action<int, bool, float> FlipLanded;

    private Rigidbody2D _body;
    private bool _wasGrounded = true;
    private float _lastAngle;
    private float _airRotation;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        bool grounded = _vehicle.IsGrounded;

        if (_wasGrounded && !grounded)
        {
            StartJump();
        }
        else if (!grounded)
        {
            TrackRotation();
        }
        else if (!_wasGrounded)
        {
            Land();
        }

        _wasGrounded = grounded;
    }

    private void StartJump()
    {
        _airRotation = 0f;
        _lastAngle = _body.rotation;
    }

    private void TrackRotation()
    {
        // DeltaAngle handles the wrap from 359 to 0, so the sum keeps growing past a full turn.
        _airRotation += Mathf.DeltaAngle(_lastAngle, _body.rotation);
        _lastAngle = _body.rotation;
    }

    private void Land()
    {
        int flips = Mathf.FloorToInt((Mathf.Abs(_airRotation) + _rotationTolerance) / 360f);
        if (flips == 0 || !IsUpright() || !IsRunBeingDriven())
        {
            return;
        }

        // Counter-clockwise is nose up while driving right, so a positive sum is a backflip.
        bool isBackflip = _airRotation > 0f;
        float nitro = flips * _nitroPerFlip + (flips - 1) * _comboBonus;

        _vehicle.AddNitro(nitro);
        FlipLanded?.Invoke(flips, isBackflip, nitro);
    }

    private bool IsUpright()
    {
        return Vector2.Angle(transform.up, Vector2.up) <= _maxLandingTilt;
    }

    private static bool IsRunBeingDriven()
    {
        return GameManager.Instance != null && GameManager.Instance.State == RunState.Playing;
    }
}
