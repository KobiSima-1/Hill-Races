using System;
using System.Collections;
using UnityEngine;

/// <summary>The states of one run.</summary>
public enum RunState
{
    Playing,
    CoastingOut,
    Crashed,
    Finished,
    GameOver
}

/// <summary>
/// Owns the run: its state, the course timer, and the coin and style totals.
/// One per course scene; a retry reloads the scene and with it a fresh GameManager.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene references")]
    [SerializeField] private VehicleController _vehicle;
    [SerializeField] private FinishTrigger _finishTrigger;
    [SerializeField] private FuelSystem _fuel;

    [Header("Coast-out")]
    [Tooltip("Below this speed (units per second) the vehicle counts as stopped.")]
    [SerializeField, Min(0f)] private float _restSpeed = 0.2f;
    [Tooltip("How long the vehicle must stay stopped before the run ends.")]
    [SerializeField, Min(0f)] private float _restDuration = 1f;

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
        _fuel.Emptied += HandleFuelEmptied;
    }

    private void OnDisable()
    {
        _finishTrigger.Crossed -= HandleFinishCrossed;
        _fuel.Emptied -= HandleFuelEmptied;
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
        // Only a run still in progress can finish a wreck sliding over the line does not count.
        if (State != RunState.Playing)
        {
            return;
        }

        _vehicle.InputEnabled = false;
        SetState(RunState.Finished);
        Debug.Log($"Finished in {ElapsedTime:F2} s");
    }

    private void HandleFuelEmptied()
    {
        if (State != RunState.Playing)
        {
            return;
        }

        StartCoroutine(CoastOutRoutine());
    }

    /// <summary>
    /// Engine off, input off, and the buggy rolls on its momentum until it comes to rest.
    /// It exists so the player watches the consequence of a fuel decision made earlier.
    /// </summary>
    private IEnumerator CoastOutRoutine()
    {
        _vehicle.InputEnabled = false;
        SetState(RunState.CoastingOut);
        Debug.Log("Out of fuel - coasting");

        float stoppedFor = 0f;
        while (stoppedFor < _restDuration)
        {
            // Something else (a crash, later on) may end the run while we coast.
            if (State != RunState.CoastingOut)
            {
                yield break;
            }

            stoppedFor = _vehicle.Speed < _restSpeed ? stoppedFor + Time.deltaTime : 0f;
            yield return null;
        }

        SetState(RunState.GameOver);
        Debug.Log("Game over - out of fuel");
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
