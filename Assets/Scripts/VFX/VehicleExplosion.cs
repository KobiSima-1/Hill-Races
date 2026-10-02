using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Blows the buggy up when the driver's head hits the ground (GDD §6, §7):
/// a fireball and smoke from pooled puffs, a boom, a camera shake, the wheels flying off
/// and the body left charred. It reacts to the GameManager entering the Crashed state, not to the
/// head trigger itself, so a buggy that tips over after crossing the finish line does not explode.
/// The GameManager's crash sequence gives the player time to watch it before the game-over screen.
/// </summary>
public class VehicleExplosion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D _body;
    [SerializeField] private WheelJoint2D[] _wheelJoints;
    [Tooltip("Optional. Needs a Cinemachine Impulse Listener on the camera.")]
    [SerializeField] private CinemachineImpulseSource _impulseSource;

    [Header("Fireball")]
    [SerializeField] private DustPuff _firePuffPrefab;
    [SerializeField] private DustPuff _smokePuffPrefab;
    [SerializeField, Min(1)] private int _fireCount = 18;
    [SerializeField, Min(1)] private int _smokeCount = 12;
    [Tooltip("Fastest speed a puff is thrown out at (units per second).")]
    [SerializeField, Min(0f)] private float _burstSpeed = 6f;

    [Header("Debris")]
    [Tooltip("Upward speed added to the body (units per second).")]
    [SerializeField, Min(0f)] private float _bodyLaunchSpeed = 6f;
    [SerializeField] private float _bodySpin = 180f;
    [Tooltip("Speed the wheels fly off at (units per second).")]
    [SerializeField, Min(0f)] private float _wheelLaunchSpeed = 8f;
    [SerializeField] private Color _charredColor = new Color(0.25f, 0.22f, 0.2f);

    [Header("Feedback")]
    [SerializeField] private AudioClip _explosionSound;
    [SerializeField, Min(0f)] private float _shakeForce = 1f;

    private PoolService<DustPuff> _firePool;
    private PoolService<DustPuff> _smokePool;
    private SpriteRenderer[] _renderers;

    private void Awake()
    {
        // Pre-warmed at load time, so the frame of the crash does not pay for Instantiate.
        Transform container = new GameObject("ExplosionPool").transform;
        _firePool = new PoolService<DustPuff>(_firePuffPrefab, container, _fireCount, _fireCount);
        _smokePool = new PoolService<DustPuff>(_smokePuffPrefab, container, _smokeCount, _smokeCount);

        _renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        // Start, not OnEnable: the GameManager sets its Instance in its own Awake.
        GameManager.Instance.StateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(RunState state)
    {
        if (state == RunState.Crashed)
        {
            Explode();
        }
    }

    private void Explode()
    {
        Vector3 center = _body.worldCenterOfMass;

        Burst(_firePool, _fireCount, center);
        Burst(_smokePool, _smokeCount, center);
        LaunchDebris();
        Char();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(_explosionSound);
        }

        if (_impulseSource != null)
        {
            _impulseSource.GenerateImpulseWithForce(_shakeForce);
        }
    }

    private void Burst(PoolService<DustPuff> pool, int count, Vector3 center)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = Random.insideUnitCircle * _burstSpeed;
            DustPuff puff = pool.Get();
            puff.Play(center, velocity, pool.Release);
        }
    }

    private void LaunchDebris()
    {
        _body.linearVelocity += Vector2.up * _bodyLaunchSpeed;
        _body.angularVelocity += Random.Range(-_bodySpin, _bodySpin);

        foreach (WheelJoint2D joint in _wheelJoints)
        {
            // Without its joint the wheel is a free body and flies off on its own.
            joint.enabled = false;

            Rigidbody2D wheel = joint.connectedBody;
            Vector2 away = ((Vector2)wheel.position - _body.position).normalized;
            wheel.linearVelocity += (away + Vector2.up).normalized * _wheelLaunchSpeed;
            wheel.angularVelocity += Random.Range(-720f, 720f);
        }
    }

    private void Char()
    {
        foreach (SpriteRenderer spriteRenderer in _renderers)
        {
            spriteRenderer.color *= _charredColor;
        }
    }
}
