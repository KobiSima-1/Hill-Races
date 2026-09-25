using UnityEngine;

/// <summary>
/// What a vehicle *is*: mass, centre of mass, suspension, motor and air-rotation values.
/// Every tunable driving number lives here so a tuning pass never needs a recompile.
/// </summary>
[CreateAssetMenu(fileName = "VehicleConfig", menuName = "Hill Races/Vehicle Config")]
public class VehicleConfig : ScriptableObject
{
    [field: Header("Mass")]
    [field: SerializeField] public float BodyMass { get; private set; } = 120f;
    [field: SerializeField] public float WheelMass { get; private set; } = 15f;

    [field: Tooltip("Local offset of the body's centre of mass. Low and forward = stable, high and back = flippy.")]
    [field: SerializeField] public Vector2 CenterOfMassOffset { get; private set; } = new Vector2(0f, -0.3f);

    [field: Header("Suspension")]
    [field: Tooltip("Spring stiffness in Hz. Lower = softer, deeper travel and a visible bounce on landing.")]
    [field: SerializeField] public float SuspensionFrequency { get; private set; } = 2.5f;
    [field: Tooltip("0 = bounces forever, 1 = no bounce at all.")]
    [field: SerializeField, Range(0f, 1f)] public float SuspensionDampingRatio { get; private set; } = 0.35f;

    [field: Header("Ground drive")]
    [field: Tooltip("Wheel angular speed cap, in degrees per second.")]
    [field: SerializeField] public float MaxMotorSpeed { get; private set; } = 1600f;
    [field: SerializeField] public float MotorTorque { get; private set; } = 800f;
    [field: Tooltip("How fast the motor speed climbs toward its target, in degrees per second squared.")]
    [field: SerializeField] public float MotorRampRate { get; private set; } = 2500f;
    [field: Tooltip("Fraction of MaxMotorSpeed available in reverse.")]
    [field: SerializeField, Range(0f, 1f)] public float ReverseFraction { get; private set; } = 0.5f;

    [field: Header("Air control")]
    [field: SerializeField] public float AirTorque { get; private set; } = 220f;
}
