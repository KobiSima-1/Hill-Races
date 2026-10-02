using UnityEngine;

/// <summary>The best results ever recorded on one course.</summary>
public readonly struct BestResult
{
    public readonly bool HasFinished;
    public readonly float Time;
    public readonly Medal Medal;
    public readonly int Coins;

    public BestResult(bool hasFinished, float time, Medal medal, int coins)
    {
        HasFinished = hasFinished;
        Time = time;
        Medal = medal;
        Coins = coins;
    }
}

/// <summary>What one finished run did to the records: the bests before it, and which ones it beat.</summary>
public readonly struct FinishRecord
{
    public readonly BestResult Previous;
    public readonly bool IsNewBestTime;
    public readonly bool IsNewBestMedal;
    public readonly bool IsNewBestCoins;

    public FinishRecord(BestResult previous, bool isNewBestTime, bool isNewBestMedal, bool isNewBestCoins)
    {
        Previous = previous;
        IsNewBestTime = isNewBestTime;
        IsNewBestMedal = isNewBestMedal;
        IsNewBestCoins = isNewBestCoins;
    }
}

/// <summary>
/// Reads and writes the best time, best medal and best coin count per course, in PlayerPrefs (GDD §7).
/// Only a finished run is recorded, and each best is written only when it is beaten -
/// so a slow run with many coins still keeps its coin record.
/// </summary>
public static class SaveService
{
    private const string KeyPrefix = "HillRaces";

    public static BestResult LoadBest(string courseId)
    {
        bool hasFinished = PlayerPrefs.GetInt(Key(courseId, "Finished"), 0) == 1;
        if (!hasFinished)
        {
            return new BestResult(false, 0f, Medal.None, 0);
        }

        return new BestResult(
            true,
            PlayerPrefs.GetFloat(Key(courseId, "BestTime")),
            (Medal)PlayerPrefs.GetInt(Key(courseId, "BestMedal")),
            PlayerPrefs.GetInt(Key(courseId, "BestCoins")));
    }

    public static FinishRecord RecordFinish(string courseId, float time, Medal medal, int coins)
    {
        BestResult previous = LoadBest(courseId);

        // On the first finish everything is a new best.
        bool isNewBestTime = !previous.HasFinished || time < previous.Time;
        bool isNewBestMedal = !previous.HasFinished || medal > previous.Medal;
        bool isNewBestCoins = !previous.HasFinished || coins > previous.Coins;

        PlayerPrefs.SetInt(Key(courseId, "Finished"), 1);
        if (isNewBestTime)
        {
            PlayerPrefs.SetFloat(Key(courseId, "BestTime"), time);
        }

        if (isNewBestMedal)
        {
            PlayerPrefs.SetInt(Key(courseId, "BestMedal"), (int)medal);
        }

        if (isNewBestCoins)
        {
            PlayerPrefs.SetInt(Key(courseId, "BestCoins"), coins);
        }

        // Write to disk now - a WebGL build may be closed without a clean quit.
        PlayerPrefs.Save();

        return new FinishRecord(previous, isNewBestTime, isNewBestMedal, isNewBestCoins);
    }

    private static string Key(string courseId, string field)
    {
        return $"{KeyPrefix}.{courseId}.{field}";
    }
}
