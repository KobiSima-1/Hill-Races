using System;
using UnityEngine;

/// <summary>
/// One pooled cloud. It follows the camera most of the way (parallax), so it looks far away,
/// and drifts slowly with the wind. Once it falls behind the left edge of the view it goes back to its pool.
/// </summary>
public class Cloud : MonoBehaviour
{
    [Tooltip("1 = glued to the camera (infinitely far), 0 = fixed in the world like the ground.")]
    [SerializeField, Range(0f, 1f)] private float _parallax = 0.85f;
    [Tooltip("Drift in units per second. Negative = to the left.")]
    [SerializeField] private float _windSpeed = -0.3f;
    [Tooltip("How far past the left edge of the view before the cloud is recycled.")]
    [SerializeField, Min(0f)] private float _despawnMargin = 4f;

    private Camera _camera;
    private Vector3 _lastCameraPosition;
    private Action<Cloud> _onOffscreen;

    public void Play(Camera cam, Vector3 position, float scale, Action<Cloud> onOffscreen)
    {
        _camera = cam;
        _lastCameraPosition = cam.transform.position;
        _onOffscreen = onOffscreen;
        transform.position = position;
        transform.localScale = Vector3.one * scale;
    }

    private void LateUpdate()
    {
        // LateUpdate: the camera has already moved this frame.
        Vector3 cameraPosition = _camera.transform.position;
        Vector3 cameraDelta = cameraPosition - _lastCameraPosition;
        _lastCameraPosition = cameraPosition;

        Vector3 move = cameraDelta * _parallax;
        move.x += _windSpeed * Time.deltaTime;
        move.z = 0f;
        transform.position += move;

        float viewLeft = cameraPosition.x - _camera.orthographicSize * _camera.aspect;
        if (transform.position.x < viewLeft - _despawnMargin)
        {
            _onOffscreen?.Invoke(this);
        }
    }
}
