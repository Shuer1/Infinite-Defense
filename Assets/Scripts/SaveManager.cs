using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class SaveManager
{
    private const string HighScoreKey = "HighScore";
    private const string CurrentWaveKey = "CurrentWave";
    private const string PlayerHealthKey = "PlayerHealth";
    private static string[] IntDataKeys = { "HighScore", "CurrentWave", "PlayerHealth", "Bullet", "ExplosiveBullet", "FrostBullet" };

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
            PlayerPrefs.SetInt(CurrentWaveKey, wave);
            PlayerPrefs.Save();
        }
    }

    public static void SavePlayerHealth(int maxHealth) //保存玩家最大血量
    {
        int currentPlayerMaxHP = GetPlayerMaxHP();
        if (maxHealth > currentPlayerMaxHP)
        {
            PlayerPrefs.SetInt(PlayerHealthKey, maxHealth);
        }
    }

    public static int GetHighScore()
    {
        return PlayerPrefs.GetInt(HighScoreKey, 0);  //Getxxx(key，value)方法的第二个参数value：当键Key对应的值为空时，默认返回value
    }

    public static int GetCurrentWave()
    {
        return PlayerPrefs.GetInt(CurrentWaveKey, 1);
    }

    public static int GetPlayerMaxHP()
    {
        return PlayerPrefs.GetInt(PlayerHealthKey, 100);
    }

    public static int GetIntTypeData(string intDataKey)
    {
        return PlayerPrefs.GetInt(intDataKey, 1);
    }
    
    

}
