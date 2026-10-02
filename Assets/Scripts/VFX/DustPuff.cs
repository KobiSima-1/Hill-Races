using System;
using UnityEngine;

/// <summary>
/// One pooled puff of dust: grows, drifts upward and fades out, then hands itself back to its pool.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DustPuff : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float _lifetime = 0.5f;
    [SerializeField, Min(0f)] private float _startScale = 0.3f;
    [SerializeField, Min(0f)] private float _endScale = 0.9f;
    [Tooltip("Movement per second while the puff is alive.")]
    [SerializeField] private Vector2 _drift = new Vector2(0f, 0.6f);

    private SpriteRenderer _renderer;
    private Color _baseColor;
    private float _age;
    private Action<DustPuff> _onFinished;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _baseColor = _renderer.color;
    }

    /// <summary>Starts the puff at a position. onFinished is called once it has faded out.</summary>
    public void Play(Vector3 position, Action<DustPuff> onFinished)
    {
        transform.position = position;
        _age = 0f;
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

        transform.position += (Vector3)(_drift * Time.deltaTime);
        ApplyProgress(progress);
    }

    private void ApplyProgress(float progress)
    {
        transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, progress);

        Color color = _baseColor;
        color.a = _baseColor.a * (1f - progress);
        _renderer.color = color;
    }
}
