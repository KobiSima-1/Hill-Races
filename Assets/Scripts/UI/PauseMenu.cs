using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Esc (or Start on a gamepad) freezes the run and opens the pause panel (GDD §5).
/// Pausing sets Time.timeScale to 0, which stops physics, the clock and the fuel drain,
/// and pauses every AudioSource, including the persistent AudioManager.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameManager _game;
    [SerializeField] private GameObject _pausePanel;
    [Tooltip("Selected when the panel opens, so a gamepad can press it straight away.")]
    [SerializeField] private GameObject _firstSelected;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        _pausePanel.SetActive(false);
    }

    private void Update()
    {
        if (!WasPausePressed())
        {
            return;
        }

        if (IsPaused)
        {
            Resume();
        }
        else if (_game.IsRunInProgress)
        {
            // No pausing on the end screens: there is nothing left to freeze.
            Pause();
        }
    }

    private void OnDestroy()
    {
        // timeScale and AudioListener.pause are global and survive a scene load.
        // Restarting from the pause panel (or with R) must not start the next run frozen.
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    /// <summary>Hooked to the Resume button's OnClick in the Inspector.</summary>
    public void OnResumeClicked()
    {
        Resume();
    }

    /// <summary>Hooked to the Restart button's OnClick in the Inspector.</summary>
    public void OnRestartClicked()
    {
        _game.RestartCourse();
    }

    /// <summary>Hooked to the Menu button's OnClick in the Inspector.</summary>
    public void OnMenuClicked()
    {
        SceneLoader.LoadMenu();
    }

    private void Pause()
    {
        SetPaused(true);
        EventSystem.current.SetSelectedGameObject(_firstSelected);
    }

    private void Resume()
    {
        SetPaused(false);
    }

    private void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
        _pausePanel.SetActive(paused);
    }

    private static bool WasPausePressed()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame))
            || (gamepad != null && gamepad.startButton.wasPressedThisFrame);
    }
}
