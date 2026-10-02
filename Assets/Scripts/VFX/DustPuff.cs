using System;
using UnityEngine;

/// <summary>
/// One pooled puff: grows, drifts and fades out, then hands itself back to its pool.
/// The same script drives the wheel dust and the fire and smoke of the explosion;
/// each prefab sets its own size, lifetime and colour.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DustPuff : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float _lifetime = 0.5f;
    [SerializeField, Min(0f)] private float _startScale = 0.3f;
    [SerializeField, Min(0f)] private float _endScale = 0.9f;
    [Tooltip("Movement per second while the puff is alive, on top of the launch velocity.")]
    [SerializeField] private Vector2 _drift = new Vector2(0f, 0.6f);
    [Tooltip("How quickly the launch velocity dies out. Higher = the puff stops sooner.")]
    [SerializeField, Min(0f)] private float _velocityDamping = 3f;

    [Header("Colour")]
    [Tooltip("Off: the sprite colour fades out. On: the colour follows the gradient (fire turning to smoke).")]
    [SerializeField] private bool _useColorOverLife;
    [SerializeField] private Gradient _colorOverLife = new Gradient();

    private SpriteRenderer _renderer;
    private Color _baseColor;
    private float _age;
    private Vector2 _velocity;
    private Action<DustPuff> _onFinished;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _baseColor = _renderer.color;
    }

    /// <summary>Starts a puff that only drifts. onFinished is called once it has faded out.</summary>
    public void Play(Vector3 position, Action<DustPuff> onFinished)
    {
        Play(position, Vector2.zero, onFinished);
    }

    /// <summary>Starts a puff thrown out with a velocity, like the pieces of an explosion.</summary>
    public void Play(Vector3 position, Vector2 velocity, Action<DustPuff> onFinished)
    {
        transform.position = position;
        _age = 0f;
        _velocity = velocity;
        _onFinished = onFinished;
        ApplyProgress(0f);
    }

    private void Update()
    {
        _age += Time.deltaTime;
        float progress = _age / _lifetime;

        if (progress >= 1f)
        {
            _onFinished?.Invoke(this);
            return;
        }

        _velocity *= Mathf.Exp(-_velocityDamping * Time.deltaTime);
        transform.position += (Vector3)((_velocity + _drift) * Time.deltaTime);
        ApplyProgress(progress);
    }

    private void ApplyProgress(float progress)
    {
        transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, progress);

        if (_useColorOverLife)
        {
            _renderer.color = _colorOverLife.Evaluate(progress);
            return;
        }

        Color color = _baseColor;
        color.a = _baseColor.a * (1f - progress);
        _renderer.color = color;
    }
}
