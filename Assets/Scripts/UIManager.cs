using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public Slider playerHP;
    public TextMeshProUGUI maxHPText;
    public TextMeshProUGUI attack;
    public TextMeshProUGUI exp;
    public TextMeshProUGUI propCount;
    [Header("Game Over Info")]
    public GameObject gameOverPanel;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ShowhighScore();
    }

    public void UpdateScore(int score)
    {
        scoreText.text = "Score: " + score;
    }

    public void UpdateAndShowPlayerHP(int currentHP, int maxHP)
    {
        int tempMaxHP = maxHP;
        maxHPText.text = tempMaxHP.ToString();

        if (maxHP <= 0) maxHP = 1;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        float hpPercentage = (float)currentHP / maxHP * 100f; //百分比后的值
        playerHP.value = hpPercentage;
    }

    public void ShowAndUpdatePlayerAttack(int newAttack) //统一更新和显示玩家额外信息方法
    {
        attack.text = newAttack.ToString();
    }

    public void ShowAndUpdatePlayerExp(int currentExp, int nextLevelExp) //统一更新和显示玩家额外信息方法
    {
        exp.text = currentExp.ToString() + " / " + nextLevelExp.ToString();
    }

    private void ShowhighScore()  //用于初始化显示历史最高分数
    {
        int current_highScore = DataManager.GetInt(DataManager.HighScoreKey);
        highScoreText.text = "High Score: " + current_highScore;
    }

    public void ShowAndUpdatePropCount(int currentPropCount)
    {
        propCount.text = currentPropCount.ToString();
    }

    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
    }
}
