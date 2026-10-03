using System;
using UnityEngine;

/// <summary>
/// Counts full rotations during a jump and rewards a clean landing with nitro (GDD §3).
///
/// A jump is judged only once the buggy has settled on both wheels, not at the first touch:
/// a short bounce or a wheel scraping the ground mid-flip does not end the jump, and a landing
/// that ends on the roof never settles, so it earns nothing.
/// A flip counts as clean only if the wheels touched the ground first: landing on the nose
/// or the tail spoils it. Once a wheel is down, a hard landing that bottoms out the suspension
/// and scrapes the body is still fine.
/// Several flips in one jump are a combo, and each extra flip adds a bonus on top.
/// </summary>
// Runs after VehicleController, so IsGrounded is from this physics step and not the one before.
// Otherwise, on a hard landing, the body contact is seen one step before the wheel contact.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody2D))]
public class FlipTracker : MonoBehaviour
{
    [SerializeField] private VehicleController _vehicle;
    [Tooltip("The ground layer. The body touching it during a jump spoils the flip.")]
    [SerializeField] private LayerMask _groundLayer;

    [Header("What counts as a jump")]
    [Tooltip("Seconds in the air before a jump can earn anything. Filters out small bumps.")]
    [SerializeField, Min(0f)] private float _minAirTime = 0.4f;
    [Tooltip("Seconds on both wheels before the landing is judged.")]
    [SerializeField, Min(0f)] private float _settleTime = 0.2f;

    [Header("What counts as a clean flip")]
    [Tooltip("How far the total rotation may be from a whole number of turns, because take-off and landing slopes differ.")]
    [SerializeField, Range(0f, 90f)] private float _rotationTolerance = 60f;

    [Header("Debug")]
    [Tooltip("Logs every judged jump and why it did or did not count.")]
    [SerializeField] private bool _logJumps;

    [Header("Reward")]
    [Tooltip("Seconds of nitro for each flip.")]
    [SerializeField, Min(0f)] private float _nitroPerFlip = 1.5f;
    [Tooltip("Extra seconds for every flip after the first in the same jump.")]
    [SerializeField, Min(0f)] private float _comboBonus = 1f;

    /// <summary>Raised on a clean landing: number of flips, true for backflips, and the nitro earned.</summary>
    public event Action<int, bool, float> FlipLanded;

    private Rigidbody2D _body;
    private float _lastAngle;
    private float _rotation;
    private float _airTime;
    private float _settledFor;
    private bool _bodyHitGround;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _lastAngle = _body.rotation;
    }

    private void FixedUpdate()
    {
        // DeltaAngle handles the wrap from 359 to 0, so the sum keeps growing past a full turn.
        // Rotation on the ground counts too: a backflip usually starts as a wheelie before take-off.
        _rotation += Mathf.DeltaAngle(_lastAngle, _body.rotation);
        _lastAngle = _body.rotation;

        // The body's own colliders, not the wheels (they are separate bodies): the nose, the tail or the roof.
        // It only counts while no wheel is down: after the take-off phase, so scraping the lip of the
        // ramp on the way up is fine, and before the wheels land, so a hard landing is fine too.
        bool bodyLandedFirst = _airTime >= _minAirTime && !_vehicle.IsGrounded
                               && _body.IsTouchingLayers(_groundLayer);
        if (bodyLandedFirst)
        {
            _bodyHitGround = true;
        }

        if (!_vehicle.IsGrounded)
        {
            _airTime += Time.fixedDeltaTime;
            _settledFor = 0f;
            return;
        }

        if (!_vehicle.AreBothWheelsGrounded)
        {
            _settledFor = 0f;
            return;
        }

        _settledFor += Time.fixedDeltaTime;
        if (_settledFor < _settleTime)
        {
            return;
        }

        if (_airTime > 0f)
        {
            JudgeLanding();
        }

        // Settled and driving: start counting the next jump from here.
        _rotation = 0f;
        _airTime = 0f;
        _bodyHitGround = false;
    }

    private void JudgeLanding()
    {
        if (_logJumps)
        {
            Debug.Log($"Jump: air {_airTime:F2}s, rotation {_rotation:F0}, body landed first {_bodyHitGround}");
        }

        if (_airTime < _minAirTime || _bodyHitGround || !IsRunBeingDriven())
        {
            return;
        }

        float turns = Mathf.Abs(_rotation) / 360f;
        int flips = Mathf.RoundToInt(turns);
        float missedBy = Mathf.Abs(turns - flips) * 360f;
        if (flips == 0 || missedBy > _rotationTolerance)
        {
            return;
        }

        // Counter-clockwise is nose up while driving right, so a positive sum is a backflip.
        bool isBackflip = _rotation > 0f;
        float nitro = flips * _nitroPerFlip + (flips - 1) * _comboBonus;

        _vehicle.AddNitro(nitro);
        FlipLanded?.Invoke(flips, isBackflip, nitro);
    }

    private static bool IsRunBeingDriven()
    {
        return GameManager.Instance != null && GameManager.Instance.State == RunState.Playing;
    }
}
