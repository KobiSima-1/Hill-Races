using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>The states of one run (GDD §3 state diagram).</summary>
public enum RunState
{
    Playing,
    CoastingOut,
    Crashed,
    Finished,
    GameOver
}

/// <summary>Why a run ended without finishing. The game-over screen shows it (GDD §5).</summary>
public enum RunEndReason
{
    None,
    Crashed,
    OutOfFuel
}

/// <summary>
/// Owns the run: its state, the course timer, and the coin and style totals (GDD §7).
/// One per course scene; a retry reloads the scene and with it a fresh GameManager.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene references")]
    [SerializeField] private VehicleController _vehicle;
    [SerializeField] private FinishTrigger _finishTrigger;
    [SerializeField] private FuelSystem _fuel;
    [SerializeField] private CrashDetector _crashDetector;
    [SerializeField] private CourseConfig _courseConfig;

    [Header("Coast-out")]
    [Tooltip("Rolling resistance added once the engine cuts, so the buggy slows to a stop instead of rocking forever.")]
    [SerializeField, Min(0f)] private float _coastDrag = 1f;
    [Tooltip("Below this speed (units per second) the vehicle counts as stopped.")]
    [SerializeField, Min(0f)] private float _restSpeed = 0.3f;
    [Tooltip("How long the vehicle must stay stopped before the run ends.")]
    [SerializeField, Min(0f)] private float _restDuration = 1f;
    [Tooltip("Safety net: the run ends after this long even if the vehicle is still moving.")]
    [SerializeField, Min(0f)] private float _maxCoastDuration = 8f;

    [Header("Crash")]
    [Tooltip("Time between the head hitting the ground and the game-over screen (GDD §3).")]
    [SerializeField, Min(0f)] private float _crashSequenceDuration = 1.5f;

    public RunState State { get; private set; } = RunState.Playing;
    public RunEndReason EndReason { get; private set; } = RunEndReason.None;
    public float ElapsedTime { get; private set; }
    public int Coins { get; private set; }

    /// <summary>How the last finish compared to the saved records. Set just before the Finished state.</summary>
    public FinishRecord LastFinish { get; private set; }

    /// <summary>True while the run can still end in a finish: driving, or coasting on the last of the momentum.</summary>
    public bool IsRunInProgress => State == RunState.Playing || State == RunState.CoastingOut;

    /// <summary>Raised whenever the run changes state. UI, camera and audio listen to this.</summary>
    public event Action<RunState> StateChanged;

    /// <summary>Raised with the new coin total whenever a coin is collected. The HUD listens.</summary>
    public event Action<int> CoinsChanged;

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
        _crashDetector.Crashed += HandleCrashed;
    }

    private void OnDisable()
    {
        _finishTrigger.Crossed -= HandleFinishCrossed;
        _fuel.Emptied -= HandleFuelEmptied;
        _crashDetector.Crashed -= HandleCrashed;
    }

    private void Update()
    {
        // The clock keeps running while coasting: a run that rolls over the line still has a time.
        if (IsRunInProgress)
        {
            ElapsedTime += Time.deltaTime;
        }

        if (WasRestartPressed())
        {
            RestartCourse();
        }
    }

    /// <summary>Reloads the course from the start. R restarts from anywhere, with no confirmation (GDD §4).</summary>
    public void RestartCourse()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Adds a coin's value to the run's score. Returns false once the run is over.</summary>
    public bool TryCollectCoin(int value)
    {
        if (!IsRunInProgress)
        {
            return false;
        }

        Coins += value;
        CoinsChanged?.Invoke(Coins);
        return true;
    }

    /// <summary>Refuels by one can. Returns false if the can was not used (the tank already ran dry).</summary>
    public bool TryCollectFuelCan()
    {
        return _fuel.TryAddCan();
    }

    private static bool WasRestartPressed()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            || (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);
    }

    private void HandleFinishCrossed()
    {
        // A buggy that runs dry and rolls over the line on momentum still finishes.
        // A wreck sliding over the line after a crash does not.
        if (!IsRunInProgress)
        {
            return;
        }

        _vehicle.InputEnabled = false;

        // Save before announcing the finish, so the results screen can show the new records.
        Medal medal = _courseConfig.GetMedal(ElapsedTime);
        LastFinish = SaveService.RecordFinish(SceneManager.GetActiveScene().name, ElapsedTime, medal, Coins);

        SetState(RunState.Finished);
        Debug.Log($"Finished in {ElapsedTime:F2} s with {Coins} coins");
    }

    private void HandleFuelEmptied()
    {
        if (State != RunState.Playing)
        {
            return;
        }

        StartCoroutine(CoastOutRoutine());
    }

    private void HandleCrashed()
    {
        // A crash can end a normal run or a coast-out, but not a run that has already finished.
        if (!IsRunInProgress)
        {
            return;
        }

        StartCoroutine(CrashRoutine());
    }

    /// <summary>
    /// Engine off, input off, and the buggy rolls on its momentum until it comes to rest (GDD §3).
    /// It exists so the player watches the consequence of a fuel decision made earlier.
    /// </summary>
    private IEnumerator CoastOutRoutine()
    {
        _vehicle.InputEnabled = false;
        _vehicle.ApplyCoastDrag(_coastDrag);
        SetState(RunState.CoastingOut);
        Debug.Log($"Out of fuel after {ElapsedTime:F1} s at x = {_vehicle.transform.position.x:F0} - coasting");

        float stoppedFor = 0f;
        float coastingFor = 0f;
        while (stoppedFor < _restDuration && coastingFor < _maxCoastDuration)
        {
            // A crash or the finish line may end the run while we coast.
            if (State != RunState.CoastingOut)
            {
                yield break;
            }

            coastingFor += Time.deltaTime;
            stoppedFor = _vehicle.Speed < _restSpeed ? stoppedFor + Time.deltaTime : 0f;
            yield return null;
        }

        EndRun(RunEndReason.OutOfFuel);
    }

    /// <summary>
    /// Input off, then a short pause so the player sees the crash before the game-over screen (GDD §3).
    /// The camera shake, dust burst and engine-die sound hook into the Crashed state in step 7.
    /// </summary>
    private IEnumerator CrashRoutine()
    {
        _vehicle.InputEnabled = false;
        SetState(RunState.Crashed);
        Debug.Log($"Crashed after {ElapsedTime:F1} s at x = {_vehicle.transform.position.x:F0}");

        yield return new WaitForSeconds(_crashSequenceDuration);

        EndRun(RunEndReason.Crashed);
    }

    private void EndRun(RunEndReason reason)
    {
        EndReason = reason;
        SetState(RunState.GameOver);
        Debug.Log($"Game over - {reason}");
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
