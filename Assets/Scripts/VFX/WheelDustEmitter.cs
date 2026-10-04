using UnityEngine;

/// <summary>
/// Kicks up dust behind each wheel that is on the ground while the buggy is moving.
/// Dozens of puffs per second for the whole run - this is the case the object pool exists for.
/// </summary>
public class WheelDustEmitter : MonoBehaviour
{
    [SerializeField] private DustPuff _puffPrefab;
    [SerializeField] private VehicleController _vehicle;
    [SerializeField] private CircleCollider2D[] _wheels;
    [SerializeField] private LayerMask _groundLayer;

    [Header("Emission")]
    [Tooltip("No dust below this speed (units per second).")]
    [SerializeField, Min(0f)] private float _minSpeed = 2f;
    [Tooltip("Seconds between puffs from each wheel.")]
    [SerializeField, Min(0.01f)] private float _interval = 0.06f;

    [Header("Pool")]
    [SerializeField, Min(0)] private int _prewarmCount = 30;
    [SerializeField, Min(1)] private int _maxPoolSize = 100;

    private PoolService<DustPuff> _pool;
    private float _timer;

    private void Awake()
    {
        // The container lives at the scene root: puffs must stay where they were dropped,
        // not follow the buggy around as children of it.
        Transform container = new GameObject("DustPool").transform;
        _pool = new PoolService<DustPuff>(_puffPrefab, container, _prewarmCount, _maxPoolSize);
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f || _vehicle.Speed < _minSpeed)
        {
            return;
        }

        _timer = _interval;
        foreach (CircleCollider2D wheel in _wheels)
        {
            if (wheel.IsTouchingLayers(_groundLayer))
            {
                EmitAt(GetContactPoint(wheel));
            }
        }
    }

    private void EmitAt(Vector3 position)
    {
        DustPuff puff = _pool.Get();
        puff.Play(position, _pool.Release);
    }

    private static Vector3 GetContactPoint(CircleCollider2D wheel)
    {
        // The bottom of the wheel: its centre minus its world-space radius.
        Bounds bounds = wheel.bounds;
        return bounds.center - new Vector3(0f, bounds.extents.y, 0f);
    }
}
