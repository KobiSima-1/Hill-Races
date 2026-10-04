using System;
using UnityEngine;

/// <summary>
/// Detects the vehicle crossing the finish line and reports it once.
/// It only raises an event; deciding what finishing means is the GameManager's job.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FinishTrigger : MonoBehaviour
{
    private bool _crossed;

    /// <summary>Raised the first time any part of the vehicle enters the trigger.</summary>
    public event Action Crossed;

    private void Reset()
    {
        // Runs when the component is first added: a finish line is always a trigger.
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // The body and both wheels each have a collider, so this can fire three times.
        if (_crossed || !VehicleColliders.Contains(other))
        {
            return;
        }

        _crossed = true;
        Debug.Log("Finish line crossed");
        Crossed?.Invoke();
    }
}
