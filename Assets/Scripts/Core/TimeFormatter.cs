/// <summary>Formats a run time the same way everywhere: m:ss.ff (for example 0:57.32).</summary>
public static class TimeFormatter
{
    public static string Format(float seconds)
    {
        int minutes = (int)(seconds / 60f);
        float remainder = seconds - minutes * 60f;
        return $"{minutes}:{remainder:00.00}";
    }
}
