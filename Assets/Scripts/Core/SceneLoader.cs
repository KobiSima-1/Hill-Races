using UnityEngine.SceneManagement;

/// <summary>
/// The one place that knows the scene names, so a renamed scene is fixed in one line.
/// The names must match the scenes in File > Build Profiles > Scene List.
/// </summary>
public static class SceneLoader
{
    public const string MenuScene = "Menu";
    public const string FirstCourseScene = "Course01";

    public static void LoadMenu()
    {
        SceneManager.LoadScene(MenuScene);
    }

    public static void LoadCourse(string courseScene)
    {
        SceneManager.LoadScene(courseScene);
    }
}
