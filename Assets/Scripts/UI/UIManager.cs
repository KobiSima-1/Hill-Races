using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds the HUD and the end-of-run screens to the GameManager and FuelSystem events (GDD §5).
/// It only displays; it never changes the run. The retry button asks the GameManager to restart.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private GameManager _game;
    [SerializeField] private FuelSystem _fuel;
    [SerializeField] private CourseConfig _courseConfig;
    [SerializeField] private Transform _vehicle;
    [SerializeField] private Transform _finishLine;

    [Header("HUD")]
    [SerializeField] private GameObject _hudPanel;
    [SerializeField] private Image _fuelFill;
    [SerializeField] private TMP_Text _clockText;
    [SerializeField] private TMP_Text _coinsText;

    [Header("Fuel gauge colours")]
    [SerializeField] private Color _fuelNormalColor = new Color(0.45f, 0.85f, 0.3f);
    [SerializeField] private Color _fuelLowColor = new Color(1f, 0.7f, 0.1f);
    [SerializeField] private Color _fuelCriticalColor = new Color(0.9f, 0.2f, 0.15f);
    [SerializeField, Range(0f, 1f)] private float _fuelLowThreshold = 0.3f;
    [SerializeField, Range(0f, 1f)] private float _fuelCriticalThreshold = 0.15f;

    [Header("Results screen")]
    [SerializeField] private GameObject _resultsPanel;
    [SerializeField] private TMP_Text _resultsTimeText;
    [SerializeField] private TMP_Text _resultsMedalText;
    [SerializeField] private TMP_Text _resultsCoinsText;
    [SerializeField] private TMP_Text _resultsBestText;

    [Header("Game-over screen")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private TMP_Text _gameOverReasonText;
    [SerializeField] private TMP_Text _gameOverDistanceText;

    [Header("End screens")]
    [Tooltip("Buttons on the end screens. They wait out the lockout so a held key cannot dismiss the screen (GDD §4).")]
    [SerializeField] private Button[] _endScreenButtons;
    [SerializeField, Min(0f)] private float _endScreenInputLockout = 1f;

    private float _startX;

    private void Awake()
    {
        _resultsPanel.SetActive(false);
        _gameOverPanel.SetActive(false);
        _hudPanel.SetActive(true);
    }

    private void OnEnable()
    {
        _game.StateChanged += HandleStateChanged;
        _game.CoinsChanged += ShowCoins;
        _fuel.Changed += ShowFuel;
    }

    private void OnDisable()
    {
        _game.StateChanged -= HandleStateChanged;
        _game.CoinsChanged -= ShowCoins;
        _fuel.Changed -= ShowFuel;
    }

    private void Start()
    {
        // Start runs after every Awake, so the fuel tank and the vehicle position are ready.
        _startX = _vehicle.position.x;
        ShowFuel(_fuel.Normalized);
        ShowCoins(_game.Coins);
    }

    private void Update()
    {
        _clockText.text = FormatTime(_game.ElapsedTime);
    }

    /// <summary>Hooked to the Retry buttons' OnClick in the Inspector.</summary>
    public void OnRetryClicked()
    {
        _game.RestartCourse();
    }

    private void HandleStateChanged(RunState state)
    {
        if (state == RunState.Finished)
        {
            ShowResults();
        }
        else if (state == RunState.GameOver)
        {
            ShowGameOver();
        }
    }

    private void ShowFuel(float normalized)
    {
        // Shrink the fill by moving its right anchor, so the width matches the fuel exactly.
        _fuelFill.rectTransform.anchorMax = new Vector2(normalized, 1f);

        if (normalized <= _fuelCriticalThreshold)
        {
            _fuelFill.color = _fuelCriticalColor;
        }
        else if (normalized <= _fuelLowThreshold)
        {
            _fuelFill.color = _fuelLowColor;
        }
        else
        {
            _fuelFill.color = _fuelNormalColor;
        }
    }

    private void ShowCoins(int coins)
    {
        _coinsText.text = coins.ToString();
    }

    private void ShowResults()
    {
        float time = _game.ElapsedTime;
        Medal medal = _courseConfig.GetMedal(time);

        _resultsTimeText.text = $"TIME  {FormatTime(time)}";
        _resultsMedalText.text = medal == Medal.None ? "NO MEDAL" : $"{medal.ToString().ToUpper()} MEDAL";
        _resultsCoinsText.text = $"COINS  {_game.Coins}";
        _resultsBestText.text = GetBestLine(_game.LastFinish);

        OpenEndScreen(_resultsPanel);
    }

    private void ShowGameOver()
    {
        _gameOverReasonText.text = _game.EndReason == RunEndReason.OutOfFuel ? "OUT OF FUEL" : "CRASHED";
        _gameOverDistanceText.text = $"DISTANCE  {GetCourseProgress():P0}";

        OpenEndScreen(_gameOverPanel);
    }

    private void OpenEndScreen(GameObject panel)
    {
        _hudPanel.SetActive(false);
        panel.SetActive(true);
        StartCoroutine(EndScreenLockoutRoutine());
    }

    /// <summary>Buttons stay disabled for a moment, so the key held at the crash cannot also press Retry.</summary>
    private IEnumerator EndScreenLockoutRoutine()
    {
        SetEndScreenButtonsInteractable(false);
        yield return new WaitForSeconds(_endScreenInputLockout);
        SetEndScreenButtonsInteractable(true);
    }

    private void SetEndScreenButtonsInteractable(bool interactable)
    {
        foreach (Button button in _endScreenButtons)
        {
            button.interactable = interactable;
        }
    }

    /// <summary>One line under the results: a new record, or the record still standing.</summary>
    private static string GetBestLine(FinishRecord record)
    {
        if (record.IsNewBestTime)
        {
            return "NEW BEST TIME!";
        }

        return $"BEST  {FormatTime(record.Previous.Time)}";
    }

    /// <summary>How far along the course the vehicle got, from 0 (start) to 1 (finish line).</summary>
    private float GetCourseProgress()
    {
        float courseLength = _finishLine.position.x - _startX;
        float travelled = _vehicle.position.x - _startX;
        return Mathf.Clamp01(travelled / courseLength);
    }

    private static string FormatTime(float seconds)
    {
        int minutes = (int)(seconds / 60f);
        float remainder = seconds - minutes * 60f;
        return $"{minutes}:{remainder:00.00}";
    }
}
