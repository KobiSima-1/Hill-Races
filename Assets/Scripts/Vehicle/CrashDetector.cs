using System;
using UnityEngine;

/// <summary>
/// Sits on the driver's head and reports a crash. Two ways to crash:
/// the head touches the ground, or the buggy lies upside down and stuck - on its roof or its side,
/// with the head clear of the ground - which would otherwise leave the player with no way out.
/// The head collider is a trigger, so it never props the buggy up - it only detects.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CrashDetector : MonoBehaviour
{
    [SerializeField] private LayerMask _groundLayer;

    [Header("Stuck upside down")]
    [Tooltip("Tilt from upright, in degrees, beyond which the buggy counts as flipped.")]
    [SerializeField, Range(90f, 180f)] private float _flippedAngle = 110f;
    [Tooltip("Below this speed (units per second) a flipped buggy counts as stuck.")]
    [SerializeField, Min(0f)] private float _stuckSpeed = 0.5f;
    [Tooltip("How long it must stay flipped and stuck before it counts as a crash.")]
    [SerializeField, Min(0f)] private float _stuckDuration = 2f;

    private Rigidbody2D _body;
    private float _stuckFor;
    private bool _crashed;

    /// <summary>Raised once, the first time the buggy crashes.</summary>
    public event Action Crashed;

    private void Reset()
    {
        // Runs when the component is first added: the head is a sensor, not a solid part.
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        // The head is part of the buggy's body, so its collider belongs to the body's Rigidbody2D.
        _body = GetComponent<Collider2D>().attachedRigidbody;
    }

    private void FixedUpdate()
    {
        if (_crashed)
        {
            return;
        }

        bool isFlipped = Vector2.Angle(transform.up, Vector2.up) > _flippedAngle;
        bool isStuck = isFlipped && _body.linearVelocity.magnitude < _stuckSpeed;
        _stuckFor = isStuck ? _stuckFor + Time.fixedDeltaTime : 0f;

        if (_stuckFor >= _stuckDuration)
        {
            Crash();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_crashed && IsGround(other))
        {
            Crash();
        }
    }

    private void Crash()
    {
        _crashed = true;
        Crashed?.Invoke();
    }

    private bool IsGround(Collider2D other)
    {
        // A LayerMask is a set of bits, one per layer: check whether this layer's bit is on.
        int otherLayerBit = 1 << other.gameObject.layer;
        return (_groundLayer.value & otherLayerBit) != 0;
    }
}
