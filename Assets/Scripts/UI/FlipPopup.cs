using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows the flip that was just landed, for example "DOUBLE BACKFLIP!  NITRO +4.0s" (GDD §6).
/// The text pops in, holds, and fades out in a coroutine. A new flip restarts it.
/// </summary>
public class FlipPopup : MonoBehaviour
{
    [SerializeField] private FlipTracker _flipTracker;
    [SerializeField] private TMP_Text _text;

    [SerializeField, Min(0f)] private float _popDuration = 0.15f;
    [SerializeField, Min(0f)] private float _holdDuration = 1.2f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.5f;
    [SerializeField, Min(1f)] private float _popScale = 1.4f;

    private Coroutine _showRoutine;

    private void Awake()
    {
        _text.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        _flipTracker.FlipLanded += HandleFlipLanded;
    }

    private void OnDisable()
    {
        _flipTracker.FlipLanded -= HandleFlipLanded;
    }

    private void HandleFlipLanded(int flips, bool isBackflip, float nitroSeconds)
    {
        _text.text = $"{GetComboWord(flips)}{(isBackflip ? "BACKFLIP" : "FRONTFLIP")}!\nNITRO +{nitroSeconds:0.0}s";

        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
        }

        _showRoutine = StartCoroutine(ShowRoutine());
    }

    private static string GetComboWord(int flips)
    {
        switch (flips)
        {
            case 1: return "";
            case 2: return "DOUBLE ";
            case 3: return "TRIPLE ";
            default: return $"{flips}x ";
        }
    }

    private IEnumerator ShowRoutine()
    {
        _text.gameObject.SetActive(true);
        _text.alpha = 1f;

        // Pop: start big and settle to normal size.
        for (float t = 0f; t < _popDuration; t += Time.deltaTime)
        {
            _text.transform.localScale = Vector3.one * Mathf.Lerp(_popScale, 1f, t / _popDuration);
            yield return null;
        }

        _text.transform.localScale = Vector3.one;
        yield return new WaitForSeconds(_holdDuration);

        for (float t = 0f; t < _fadeDuration; t += Time.deltaTime)
        {
            _text.alpha = 1f - t / _fadeDuration;
            yield return null;
        }

        _text.gameObject.SetActive(false);
        _showRoutine = null;
    }
}
