using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int score = 0;
    private bool isGameOver;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void AddScore(int value)
    {
        score += value;
        UIManager.Instance.UpdateScore(score);
        SaveManager.SaveHighScore(score);
    }

    public void GameOver()
    {
        if (isGameOver) return;
        Debug.Log("Game Over UI Appears");
        isGameOver = true;

        SaveManager.SaveHighScore(score);
        UIManager.Instance.ShowGameOver(score, SaveManager.GetHighScore());
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
