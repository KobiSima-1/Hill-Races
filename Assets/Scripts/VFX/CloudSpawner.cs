using UnityEngine;

/// <summary>
/// Keeps the sky populated with clouds (GDD §6): a few at the start across the view,
/// then a new one just past the right edge every few seconds, at a random height and size.
/// Clouds come from an object pool and return to it when they leave the view on the left.
/// </summary>
public class CloudSpawner : MonoBehaviour
{
    [SerializeField] private Cloud[] _cloudPrefabs;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float _minInterval = 2f;
    [SerializeField, Min(0.1f)] private float _maxInterval = 5f;
    [Tooltip("Clouds placed across the view when the level starts, so the sky is not empty.")]
    [SerializeField, Min(0)] private int _startingClouds = 4;

    [Header("Placement (relative to the camera centre)")]
    [SerializeField] private float _minHeight = 1.5f;
    [SerializeField] private float _maxHeight = 4.5f;
    [SerializeField, Min(0f)] private float _spawnMargin = 4f;
    [SerializeField, Min(0.1f)] private float _minScale = 0.6f;
    [SerializeField, Min(0.1f)] private float _maxScale = 1.2f;

    [Header("Pool")]
    [SerializeField, Min(0)] private int _prewarmPerPrefab = 4;
    [SerializeField, Min(1)] private int _maxPerPrefab = 10;

    private PoolService<Cloud>[] _pools;
    private Camera _camera;
    private float _timer;

    private void Awake()
    {
        _camera = Camera.main;

        Transform container = new GameObject("CloudPool").transform;
        _pools = new PoolService<Cloud>[_cloudPrefabs.Length];
        for (int i = 0; i < _cloudPrefabs.Length; i++)
        {
            _pools[i] = new PoolService<Cloud>(_cloudPrefabs[i], container, _prewarmPerPrefab, _maxPerPrefab);
        }
    }

    private void Start()
    {
        float halfWidth = ViewHalfWidth();
        for (int i = 0; i < _startingClouds; i++)
        {
            float x = _camera.transform.position.x + Random.Range(-halfWidth, halfWidth);
            Spawn(x);
        }

        _timer = Random.Range(_minInterval, _maxInterval);
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f)
        {
            return;
        }

        _timer = Random.Range(_minInterval, _maxInterval);
        Spawn(_camera.transform.position.x + ViewHalfWidth() + _spawnMargin);
    }

    private void Spawn(float x)
    {
        PoolService<Cloud> pool = _pools[Random.Range(0, _pools.Length)];
        float y = _camera.transform.position.y + Random.Range(_minHeight, _maxHeight);
        float scale = Random.Range(_minScale, _maxScale);

        Cloud cloud = pool.Get();
        cloud.Play(_camera, new Vector3(x, y, 0f), scale, pool.Release);
    }

    private float ViewHalfWidth()
    {
        return _camera.orthographicSize * _camera.aspect;
    }
}
