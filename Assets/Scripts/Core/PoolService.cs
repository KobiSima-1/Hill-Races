using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// A generic wrapper over Unity's ObjectPool (GDD §7, course session 6).
/// Instances are created once, pre-warmed and deactivated, then reused instead of
/// being instantiated and destroyed - so a stream of short-lived effects causes no GC spikes.
/// </summary>
public class PoolService<T> where T : Component
{
    private readonly T _prefab;
    private readonly Transform _container;
    private readonly ObjectPool<T> _pool;

    public int CountActive => _pool.CountActive;
    public int CountInactive => _pool.CountInactive;

    public PoolService(T prefab, Transform container, int prewarmCount, int maxSize)
    {
        _prefab = prefab;
        _container = container;
        _pool = new ObjectPool<T>(
            createFunc: CreateItem,
            actionOnGet: item => item.gameObject.SetActive(true),
            actionOnRelease: item => item.gameObject.SetActive(false),
            actionOnDestroy: item => Object.Destroy(item.gameObject),
            collectionCheck: true,
            defaultCapacity: prewarmCount,
            maxSize: maxSize);

        Prewarm(prewarmCount);
    }

    public T Get()
    {
        return _pool.Get();
    }

    public void Release(T item)
    {
        _pool.Release(item);
    }

    private T CreateItem()
    {
        T item = Object.Instantiate(_prefab, _container);
        item.gameObject.SetActive(false);
        return item;
    }

    /// <summary>Creates the first batch up front, during loading, instead of mid-run.</summary>
    private void Prewarm(int count)
    {
        var items = new List<T>(count);
        for (int i = 0; i < count; i++)
        {
            items.Add(_pool.Get());
        }

        foreach (T item in items)
        {
            _pool.Release(item);
        }
    }
}
