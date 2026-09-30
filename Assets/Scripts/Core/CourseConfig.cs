using UnityEngine;

/// <summary>
/// What one course asks of the vehicle: its fuel budget and its medal times.
/// One asset per course, so a second course never touches the first one's tuning.
/// </summary>
[CreateAssetMenu(fileName = "CourseConfig", menuName = "Hill Races/Course Config")]
public class CourseConfig : ScriptableObject
{
    [field: Header("Fuel")]
    [field: SerializeField, Min(1f)] public float FuelCapacity { get; private set; } = 100f;
    [field: Tooltip("Fuel burned per second at all times while the run is in progress.")]
    [field: SerializeField, Min(0f)] public float FuelDrainIdle { get; private set; } = 2f;
    [field: Tooltip("Extra fuel burned per second while the throttle is held.")]
    [field: SerializeField, Min(0f)] public float FuelDrainThrottle { get; private set; } = 2f;
    [field: Tooltip("Fuel restored by one can. Anything above capacity is wasted.")]
    [field: SerializeField, Min(0f)] public float FuelPerCan { get; private set; } = 35f;

    [field: Header("Medal times (seconds)")]
    [field: Tooltip("Set after the course is playable - never guessed in advance.")]
    [field: SerializeField, Min(0f)] public float GoldTime { get; private set; } = 30f;
    [field: SerializeField, Min(0f)] public float SilverTime { get; private set; } = 40f;
    [field: SerializeField, Min(0f)] public float BronzeTime { get; private set; } = 55f;
}
