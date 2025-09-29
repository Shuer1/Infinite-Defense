using System.Threading.Tasks;
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

        //分数每增加就调用保存方法
        //SaveManager.SaveHighScore(score);
        DataManager.SaveInt(DataManager.HighScoreKey,score);

    }

    public void GameOver()
    {
        if (isGameOver) return;
        Debug.Log("Game Over UI Appears");
        isGameOver = true;

        DataManager.FlushSave();
        UIManager.Instance.ShowGameOver();
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
