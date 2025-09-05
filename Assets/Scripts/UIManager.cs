using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public Text scoreText;
    public Text highScoreText;
    public GameObject gameOverPanel;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateScore(int score)
    {
        scoreText.text = "Score: " + score;
    }

    public void ShowGameOver(int score, int highScore)
    {
        gameOverPanel.SetActive(true);
        scoreText.text = "Score: " + score;
        highScoreText.text = "High Score: " + highScore;
    }
}
