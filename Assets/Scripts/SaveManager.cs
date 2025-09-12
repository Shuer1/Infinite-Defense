using UnityEngine;

public static class SaveManager
{
    private const string HighScoreKey = "HighScore";

    public static void SaveHighScore(int score)
    {
        int currentHighScore = GetHighScore();
        if (score > currentHighScore)
        {
            PlayerPrefs.SetInt(HighScoreKey, score);
            PlayerPrefs.Save();
        }
    }

    public static int GetHighScore()
    {
        return PlayerPrefs.GetInt(HighScoreKey,0);  //Get(int、string...)方法的第二个参数：当键Key对应的值为空时，默认返回它
    }
}
