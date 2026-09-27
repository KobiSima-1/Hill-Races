using System;
using UnityEngine;

/// <summary>The states of one run (GDD §3 state diagram).</summary>
public enum RunState
{
    Playing,
    CoastingOut,
    Crashed,
    Finished,
    GameOver
}

/// <summary>
/// Owns the run: its state, the course timer, and the coin and style totals (GDD §7).
/// One per course scene; a retry reloads the scene and with it a fresh GameManager.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private VehicleController _vehicle;
    [SerializeField] private FinishTrigger _finishTrigger;

    public RunState State { get; private set; } = RunState.Playing;
    public float ElapsedTime { get; private set; }

    /// <summary>Raised whenever the run changes state. UI and audio listen to this.</summary>
    public event Action<RunState> StateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("A second GameManager was found and destroyed.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        _finishTrigger.Crossed += HandleFinishCrossed;
    }

    private void OnDisable()
    {
        _finishTrigger.Crossed -= HandleFinishCrossed;
    }

    private void Update()
    {
        if (State == RunState.Playing)
        {
            ElapsedTime += Time.deltaTime;
        }
    }

    private void HandleFinishCrossed()
    {
        // Only a run still in progress can finish - a wreck sliding over the line does not count.
        if (State != RunState.Playing)
        {
            return;
        }

        _vehicle.InputEnabled = false;
        SetState(RunState.Finished);
        Debug.Log($"Finished in {ElapsedTime:F2} s");
    }

    private void SetState(RunState newState)
    {
        if (State == newState)
        {
            return;
        }

        State = newState;
        StateChanged?.Invoke(newState);
    }
}
