using TMPro;
using UnityEngine;

/// <summary>
/// The first screen of the game (GDD §5): start the course, see the saved bests, or quit.
/// The bests are read from PlayerPrefs through SaveService, under the course's scene name.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string _courseScene = SceneLoader.FirstCourseScene;
    [SerializeField] private TMP_Text _bestText;
    [Tooltip("Hidden in a WebGL build, where a page cannot close itself.")]
    [SerializeField] private GameObject _quitButton;

    private void Start()
    {
        ShowBest();

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

    /// <summary>Hooked to the Quit button's OnClick in the Inspector.</summary>
    public void OnQuitClicked()
    {
        Application.Quit();
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
        _bestText.text = $"BEST TIME  {TimeFormatter.Format(best.Time)}\n{medal}\nMOST COINS  {best.Coins}";
    }
}
