using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

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
    public TextMeshProUGUI tempCurrentWaveTMP;
    private Coroutine _currentShowCoroutine;
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

    public void UpdateAndShowTowerHP(int currentTowerHP,int towerMaxHP)
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

    public void AutoShowCurrentWaveUI(Image uiPartImg, int currentWaveOrder)
    {
        Time.timeScale = 1f;

        // 强制终止并清理上次协程
        if (_currentShowCoroutine != null)
        {
            StopCoroutine(_currentShowCoroutine);
            _currentShowCoroutine = null;
        }

        // 强制重置透明度与激活状态
        if (uiPartImg != null)
        {
            Color imgColor = uiPartImg.color;
            uiPartImg.color = new Color(imgColor.r, imgColor.g, imgColor.b, 0f);
            uiPartImg.gameObject.SetActive(true);
        }

        if (tempCurrentWaveTMP != null)
        {
            Color txtColor = tempCurrentWaveTMP.color;
            tempCurrentWaveTMP.color = new Color(txtColor.r, txtColor.g, txtColor.b, 0f);
            tempCurrentWaveTMP.gameObject.SetActive(true);
        }

        // 启动新协程
        _currentShowCoroutine = StartCoroutine(ShowWaveUIFade(uiPartImg, currentWaveOrder));
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

    private IEnumerator ShowWaveUIFade(Image uiImg, int waveOrderTMP)
    {
        if (uiImg == null || tempCurrentWaveTMP == null)
        {
            Debug.LogWarning("UI图片或文本组件未赋值！");
            _currentShowCoroutine = null;
            yield break;
        }

        tempCurrentWaveTMP.text = /*waveOrderTMP.ToString();*/ $": {waveOrderTMP}";

        Color originalImgColor = uiImg.color;
        Color originalTxtColor = tempCurrentWaveTMP.color;

        // ✅ 每次开场强制置透明（防止上次残留）
        uiImg.color = new Color(originalImgColor.r, originalImgColor.g, originalImgColor.b, 0f);
        tempCurrentWaveTMP.color = new Color(originalTxtColor.r, originalTxtColor.g, originalTxtColor.b, 0f);

        // ✅ 如果对象被禁用，重新激活
        if (!uiImg.gameObject.activeInHierarchy) uiImg.gameObject.SetActive(true);
        if (!tempCurrentWaveTMP.gameObject.activeInHierarchy) tempCurrentWaveTMP.gameObject.SetActive(true);

        // 淡入
        float fadeInTime = 0.3f;
        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInTime);
            uiImg.color = new Color(originalImgColor.r, originalImgColor.g, originalImgColor.b, alpha);
            tempCurrentWaveTMP.color = new Color(originalTxtColor.r, originalTxtColor.g, originalTxtColor.b, alpha);
            yield return null;
        }

        // 显示停留
        yield return new WaitForSeconds(0.9f);

        // 淡出
        float fadeOutTime = 0.3f;
        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutTime);
            uiImg.color = new Color(originalImgColor.r, originalImgColor.g, originalImgColor.b, alpha);
            tempCurrentWaveTMP.color = new Color(originalTxtColor.r, originalTxtColor.g, originalTxtColor.b, alpha);
            yield return null;
        }

        // ✅ 结束后保持激活（避免下一次协程找不到对象）
        uiImg.color = new Color(originalImgColor.r, originalImgColor.g, originalImgColor.b, 0f);
        tempCurrentWaveTMP.color = new Color(originalTxtColor.r, originalTxtColor.g, originalTxtColor.b, 0f);

        _currentShowCoroutine = null;
    }
}
