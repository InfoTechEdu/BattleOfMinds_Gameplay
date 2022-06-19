using UnityEngine;

public static class GameFilePaths
{
    public static string QuestionsData = Application.streamingAssetsPath + "/all_questions_data.json";
    public static string UserProgressData = Application.streamingAssetsPath + "/user_progress_data.json";
    public static string LeaderboardData = Application.streamingAssetsPath + "/leaderboard_data.json";
    public static string SettingsData = Application.streamingAssetsPath + "/settings_data.json";

    //FOR TESTS. DELETE THIS
    public static string SessionTestData = Application.streamingAssetsPath + "/session_test.json";

}
