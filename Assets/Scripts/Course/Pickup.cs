using UnityEngine;

public enum PickupKind
{
    Coin,
    FuelCan
}

/// <summary>
/// One hand-placed coin or fuel can (GDD §3, §7). On contact with the vehicle it asks the
/// GameManager to collect it, and disappears only if the GameManager accepted it -
/// a fuel can touched after the tank ran dry stays where it is.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Pickup : MonoBehaviour
{
    [SerializeField] private PickupKind _kind = PickupKind.Coin;
    [Tooltip("Score added by this coin. Ignored for fuel cans.")]
    [SerializeField, Min(1)] private int _coinValue = 10;
    [Tooltip("Played once when the pickup is collected.")]
    [SerializeField] private AudioClip _collectSound;

    private bool _collected;

    private void Reset()
    {
        // Runs when the component is first added: a pickup is always a trigger.
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // The body and both wheels can all touch it in the same physics step.
        if (_collected || !VehicleColliders.Contains(other))
        {
            return;
        }

        if (TryCollect())
        {
            _collected = true;
            PlayCollectSound();
            gameObject.SetActive(false);
        }
    }

    private void PlayCollectSound()
    {
        // The AudioManager may be missing when a scene is tested on its own.
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(_collectSound);
        }
    }

    private bool TryCollect()
    {
        GameManager game = GameManager.Instance;
        return _kind == PickupKind.Coin
            ? game.TryCollectCoin(_coinValue)
            : game.TryCollectFuelCan();
    }
}
