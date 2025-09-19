using UnityEngine;

public static class SaveManager
{
    private const string HighScoreKey = "HighScore";
    private const string CurrentWaveKey = "CurrentWave";

    public static void SaveHighScore(int score) //保存最高分
    {
        int currentHighScore = GetHighScore();
        if (score > currentHighScore)
        {
            PlayerPrefs.SetInt(HighScoreKey, score);
            PlayerPrefs.Save();
        }
    }

    public static void SaveCurrentWave(int wave) //保存当前游戏进度（波数）
    {
        int currentWave = GetCurrentWave();
        if (wave > currentWave)
        {
            PlayerPrefs.SetInt(CurrentWaveKey,wave);
            PlayerPrefs.Save();
        }
    }

    //Test PlayerPref.Save()
    public static int TestSaveAndGetCurrentWave()
    {
        PlayerPrefs.SetInt(CurrentWaveKey, 1);
        PlayerPrefs.Save();
        return PlayerPrefs.GetInt(CurrentWaveKey);
    }

    public static int GetHighScore()
    {
        return PlayerPrefs.GetInt(HighScoreKey, 0);  //Get(int、string...)方法的第二个参数：当键Key对应的值为空时，默认返回它
    }

    public static int GetCurrentWave()
    {
        return PlayerPrefs.GetInt(CurrentWaveKey, 1);
    }
}
