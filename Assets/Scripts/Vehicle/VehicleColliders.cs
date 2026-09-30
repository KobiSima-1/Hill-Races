using UnityEngine;

/// <summary>
/// Answers one question for every trigger in the course: "is this collider part of the player's vehicle?"
/// Shared by the finish line and the pickups so the rule lives in one place.
/// </summary>
public static class VehicleColliders
{
    public static bool Contains(Collider2D other)
    {
        // A wheel has its own Rigidbody2D that is a child of the buggy, so search upward from it.
        Rigidbody2D body = other.attachedRigidbody;
        return body != null && body.GetComponentInParent<VehicleController>() != null;
    }
}
