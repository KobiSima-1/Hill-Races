using UnityEngine;

/// <summary>
/// Shoots pooled fire puffs out of the exhaust while the nitro is burning (GDD §7).
/// It reuses the FirePuff prefab of the explosion and the same PoolService.
/// </summary>
public class NitroFlame : MonoBehaviour
{
    [SerializeField] private VehicleController _vehicle;
    [SerializeField] private DustPuff _flamePrefab;
    [Tooltip("Where the flame comes out: an empty child at the back of the buggy.")]
    [SerializeField] private Transform _exhaust;

    [SerializeField, Min(0.01f)] private float _interval = 0.03f;
    [Tooltip("Speed the flame is blown backwards at (units per second).")]
    [SerializeField, Min(0f)] private float _flameSpeed = 4f;

    [Header("Pool")]
    [SerializeField, Min(0)] private int _prewarmCount = 20;
    [SerializeField, Min(1)] private int _maxPoolSize = 60;

    private PoolService<DustPuff> _pool;
    private float _timer;

    private void Awake()
    {
        Transform container = new GameObject("NitroPool").transform;
        _pool = new PoolService<DustPuff>(_flamePrefab, container, _prewarmCount, _maxPoolSize);
    }

    private void Update()
    {
        if (!_vehicle.IsNitroActive)
        {
            return;
        }

        _timer -= Time.deltaTime;
        if (_timer > 0f)
        {
            return;
        }

        _timer = _interval;
        Vector2 backwards = -_exhaust.right * _flameSpeed;
        DustPuff flame = _pool.Get();
        flame.Play(_exhaust.position, backwards, _pool.Release);
    }
}
