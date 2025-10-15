using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public Slider playerHP;
    public Slider towerHP;
    public TextMeshProUGUI maxHPText;
    public TextMeshProUGUI towerMaxHPText;
    public TextMeshProUGUI attack;
    public TextMeshProUGUI exp;
    public TextMeshProUGUI level;
    public TextMeshProUGUI propCount;
    [Header("Game Over Info")]
    public GameObject gameOverPanel;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        InitUI();
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

    public void UpdateAndShowTowerHP(int currentTowerHP,int towerMaxHP) //⚠️新增
    {
        int tempTowerMaxHP = towerMaxHP;
        towerMaxHPText.text = tempTowerMaxHP.ToString();

        if (towerMaxHP <= 0) towerMaxHP = 1;
        currentTowerHP = Mathf.Clamp(currentTowerHP, 0, towerMaxHP);

        float hpPercentage = (float)currentTowerHP / towerMaxHP * 100f; //百分比后的值
        towerHP.value = hpPercentage;
    }

    private void ShowMaxHP()
    {
        int current_MaxHP = DataManager.GetInt(DataManager.PlayerMaxHealthKey);
        maxHPText.text = current_MaxHP.ToString();
    }

    private void ShowTowerMaxHP()
    {
        int current_TowerMaxHP = DataManager.GetInt(DataManager.DefenseTowerMaxHPKey);
        towerMaxHPText.text = current_TowerMaxHP.ToString();
    }

    private void ShowhighScore()
    {
        int current_highScore = DataManager.GetInt(DataManager.HighScoreKey);
        highScoreText.text = "High Score: " + current_highScore;
    }

    public void ShowAndUpdatePlayerAttack(int newAttack) //统一更新和显示玩家额外信息方法
    {
        attack.text = newAttack.ToString();
    }

    public void ShowAndUpdatePlayerExp(int currentExp, int nextLevelExp) //统一更新和显示玩家额外信息方法
    {
        exp.text = currentExp.ToString() + " / " + nextLevelExp.ToString();
    }

    public void ShowAndUpdatePropCount(int currentPropCount)
    {
        propCount.text = currentPropCount.ToString();
    }

    public void ShowLevel(int level_value)
    {
        level.text = "LEVEL: " + level_value;
    }

    private void InitUI()
    {
        ShowMaxHP();
        ShowTowerMaxHP(); // 新增⚠️
        ShowhighScore();
        int level_value = DataManager.GetInt(DataManager.PlayerLevelKey);
        ShowLevel(level_value);
        int current_Attack = DataManager.GetInt(DataManager.BaseBulletDamageKey);
        ShowAndUpdatePlayerAttack(current_Attack);
        int current_Exp = DataManager.GetInt(DataManager.NextLevelExpKey);
        ShowAndUpdatePlayerExp(0, current_Exp);
        int current_PropCount = DataManager.GetInt(DataManager.CurrentPropCountKey);
        ShowAndUpdatePropCount(current_PropCount);
    }

    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
    }
}
