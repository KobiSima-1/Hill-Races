using System;
using UnityEngine;

/// <summary>
/// Sits on the driver's head and reports the moment it touches the ground (GDD §3 failure rule a).
/// The head collider is a trigger, so it never props the buggy up - it only detects.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CrashDetector : MonoBehaviour
{
    [SerializeField] private LayerMask _groundLayer;

    private bool _crashed;

    /// <summary>Raised once, the first time the head touches the ground.</summary>
    public event Action Crashed;

    private void Reset()
    {
        // Runs when the component is first added: the head is a sensor, not a solid part.
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_crashed || !IsGround(other))
        {
            return;
        }

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
