using TMPro;
using UnityEngine;

/// <summary>
/// The first screen of the game (GDD §5): start the course, see the saved bests, turn the music on or off, or quit.
/// The bests are read from PlayerPrefs through SaveService, under the course's scene name.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string _courseScene = SceneLoader.FirstCourseScene;
    [SerializeField] private TMP_Text _bestText;
    [Tooltip("The text on the music button, which shows the current state.")]
    [SerializeField] private TMP_Text _musicButtonText;
    [Tooltip("Hidden in a WebGL build, where a page cannot close itself.")]
    [SerializeField] private GameObject _quitButton;

    private void Start()
    {
        ShowBest();
        ShowMusicState();

        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            _quitButton.SetActive(false);
        }
    }

    /// <summary>Hooked to the Drive button's OnClick in the Inspector.</summary>
    public void OnDriveClicked()
    {
        SceneLoader.LoadCourse(_courseScene);
    }

    /// <summary>Hooked to the Music button's OnClick in the Inspector.</summary>
    public void OnMusicClicked()
    {
        AudioManager audio = AudioManager.Instance;
        if (audio == null)
        {
            return;
        }

        audio.SetMusicEnabled(!audio.IsMusicEnabled);
        ShowMusicState();
    }

    /// <summary>Hooked to the Quit button's OnClick in the Inspector.</summary>
    public void OnQuitClicked()
    {
        Application.Quit();
    }

    private void ShowMusicState()
    {
        bool enabled = AudioManager.Instance == null || AudioManager.Instance.IsMusicEnabled;
        _musicButtonText.text = enabled ? "MUSIC: ON" : "MUSIC: OFF";
    }

    private void ShowBest()
    {
        BestResult best = SaveService.LoadBest(_courseScene);
        if (!best.HasFinished)
        {
            _bestText.text = "NO RECORDS YET - REACH THE FINISH LINE!";
            return;
        }

        string medal = best.Medal == Medal.None ? "NO MEDAL" : $"{best.Medal.ToString().ToUpper()} MEDAL";
        _bestText.text = $"BEST TIME:  {TimeFormatter.Format(best.Time)}\n{medal}\nMOST COINS:  {best.Coins}";
    }
}
