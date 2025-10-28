using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StartUIDataManager : MonoBehaviour
{
    [Header("UI info配置")]
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private int highScore;
    [SerializeField] private TextMeshProUGUI highWaveText;
    [SerializeField] private int highWave;
    void Start()
    {
        InitData();
        highScoreText.text = "HighScore: " + highScore;
        highWaveText.text = "HighWave: " + highWave;
    }

    void InitData()
    {

        highScore = DataManager.GetInt(DataManager.HighScoreKey,PlayerInitialConfig.HighScore);
        highWave = DataManager.GetInt(DataManager.CurrentWaveKey,PlayerInitialConfig.CurrentWave);
    }
    
}
