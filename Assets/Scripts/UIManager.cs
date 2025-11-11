using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using Unity.VisualScripting;

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
    public TextMeshProUGUI currentWaveTMPInPause; //新增
    private int currentWaveOrderInPause = 1;
    public TextMeshProUGUI enemiesLevelTMPInPause; //新增
    private int enemiesLevel = 1;
    public TextMeshProUGUI waveToLevelUPTMP;
    public int historyClearEnemiesWave = 0;
    public SpawnManager spawnManager;
    public TextMeshProUGUI propCount;
    [Header("升级券配置")] //新增✅
    public Button useTicketBtn;
    public TextMeshProUGUI ticketCountTMP;
    public int currentTicketCount;
    public TextMeshProUGUI tempCurrentWaveTMP;
    private Coroutine _currentShowCoroutine;
    [Header("Upgrade Panel")]
    [SerializeField] private PanelScaleAnimation upgradePanelAnim;
    [Header("首杀领取面板")]
    [SerializeField] private PanelScaleAnimation firstKillPanelAnim;
    [SerializeField] private Button grantFKRewardButton;
    private EnemyType _pendingFirstKill;

    [Header("GameOver Panel")]
    public GameObject gameOverPanel;
    [Header("倒计时UI图片配置")]
    public Image[] countdownImages;
    private bool isUpgradePanelOpen = false;
    private bool isFirstKillPanelOpen = false;
    private bool isUsingTicket = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitUI();
        HideAllCountdownImages();
        InitializeUpgradePanel();
        InitializeFKRewardPanel(); //新增✅

        if (spawnManager != null)
        {
            spawnManager.OnWaveCompleted += UpdateWaveUI; //新增
        }

        if (GlobalDifficultyCurveController.Instance != null)
        {
            GlobalDifficultyCurveController.Instance.OnEnemiesLevelUp += UpdateEnemiesLevelUI;
        }
        else
        {
            Debug.LogError("GlobalDifficultyCurveController: 未找到全局难度曲线控制器！");
            return;
        }

        useTicketBtn?.onClick.AddListener(UseTicket); //新增✅

        grantFKRewardButton?.onClick.AddListener(OnFKRewardGrant);
        HideFirstKillPanel();
    }

    public void ShowFirstKillPanel(EnemyType type)
    {
        StartCoroutine(ShowFirstKillPanelDelayed(type));
    }

    private IEnumerator ShowFirstKillPanelDelayed(EnemyType type)
    {
        _pendingFirstKill = type;

        // ✅ 如果升级面板正在播放动画，等待它关闭完毕
        if (upgradePanelAnim != null && (upgradePanelAnim.IsAnimating || isUpgradePanelOpen))
        {
            upgradePanelAnim.ClosePanel();
            isUpgradePanelOpen = false;

            // 等待升级面板完全关闭
            yield return new WaitWhile(() => upgradePanelAnim.IsAnimating);
        }

        // ✅ 确保只显示首杀面板
        if (firstKillPanelAnim == null) yield break;

        Time.timeScale = 0f;
        firstKillPanelAnim.OpenPanel();
        isFirstKillPanelOpen = true;
    }

    private void OnFKRewardGrant()
    {
        // 发放奖励
        FirstKillRewardManager.Instance.GrantFirstKillReward(_pendingFirstKill);
        HideFirstKillPanel();
    }
    private void HideFirstKillPanel()
    {
        if (firstKillPanelAnim != null)
            firstKillPanelAnim.ClosePanel();
        else
            Debug.LogWarning("FirstKillRewardPanel: 未找到首杀奖励面板引用！");

        isFirstKillPanelOpen = false;

        if(Time.timeScale == 0f)
            Time.timeScale = 1f;
    }

    private void InitializeUpgradePanel()
    {
        if (upgradePanelAnim == null)
        {
            Debug.LogError("PanelScaleAnimation: 未找到升级面板引用！");
            return;
        }

        // ✅ 保证对象激活后初始化并立即隐藏
        if (!upgradePanelAnim.gameObject.activeSelf)
            upgradePanelAnim.gameObject.SetActive(true);

        // ✅ 调用其初始化方法（内部会安全设置为关闭状态）
        upgradePanelAnim.InitializePanelState();
        // ✅ 初始化后隐藏
        upgradePanelAnim.ClosePanelImmediate();
    }

    private void InitializeFKRewardPanel()
    {
        if (firstKillPanelAnim == null) return;

        if (!firstKillPanelAnim.gameObject.activeSelf)
            firstKillPanelAnim.gameObject.SetActive(true);

        firstKillPanelAnim.InitializePanelState();
        firstKillPanelAnim.ClosePanelImmediate();
    }

    public void ShowUpgradePanel()
    {
        StartCoroutine(ShowUpgradePanelDelayed());
    }

    private IEnumerator ShowUpgradePanelDelayed()
    {
        // ✅ 如果首杀面板正在播放动画或未关闭，等待它关闭完毕
        if (firstKillPanelAnim != null && (firstKillPanelAnim.IsAnimating || isFirstKillPanelOpen))
        {
            firstKillPanelAnim.ClosePanel();
            isFirstKillPanelOpen = false;

            // 等待首杀面板动画完全关闭
            yield return new WaitWhile(() => firstKillPanelAnim.IsAnimating);
        }

        // ✅ 确保只显示升级面板
        if (upgradePanelAnim == null) yield break;

        upgradePanelAnim.OpenPanel();
        isUpgradePanelOpen = true;
    }

    public void HideUpgradePanel()
    {
        upgradePanelAnim?.ClosePanel();
        isUpgradePanelOpen = false;
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
        highScoreText.text = current_highScore.ToString();
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

    public void ShowAndUpdateTicketCount(int tempTicketCount)
    {
        ticketCountTMP.text = tempTicketCount.ToString();
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
        ShowTowerMaxHP();
        ShowhighScore();
        int level_value = DataManager.GetInt(DataManager.PlayerLevelKey);
        ShowLevel(level_value);
        int current_Attack = DataManager.GetInt(DataManager.BaseBulletDamageKey);
        ShowAndUpdatePlayerAttack(current_Attack);
        int current_Exp = DataManager.GetInt(DataManager.NextLevelExpKey);
        ShowAndUpdatePlayerExp(0, current_Exp);
        int current_PropCount = DataManager.GetInt(DataManager.CurrentPropCountKey);
        ShowAndUpdatePropCount(current_PropCount);

        currentTicketCount = DataManager.GetInt(DataManager.CurrentTicketCountKey); // New Addition✅
        ShowAndUpdateTicketCount(currentTicketCount);

        currentWaveOrderInPause = DataManager.GetInt(DataManager.CurrentWaveKey);
        currentWaveTMPInPause.text = $"CURRENT WAVE: {currentWaveOrderInPause}";

        enemiesLevel = DataManager.GetInt(DataManager.EnemiesLevelKey);
        enemiesLevelTMPInPause.text = $"ENEMIES LEVEL: {enemiesLevel}";

        historyClearEnemiesWave = DataManager.GetInt(DataManager.ClearEnemiesCountKey);
        UpdateWaveToLevelUP(historyClearEnemiesWave);
    }

    void UseTicket() //使用券:获得一次升级选择机会
    {
        if (isUsingTicket) return;

        if (currentTicketCount < 1)
        {
            // 后续添加广告进入
            Debug.LogWarning("[升级券] 已用完！");
            return;
        }

        isUsingTicket = true;

        currentTicketCount--;
        DataManager.SaveIntForce(DataManager.CurrentTicketCountKey, currentTicketCount);
        ShowAndUpdateTicketCount(currentTicketCount);

        UpgradeManager.Instance.ShowUpgradeOptions(); //显示升级选项

        StartCoroutine(ResetTicketUseCooldown());
    }

    private IEnumerator ResetTicketUseCooldown()
    {
        yield return new WaitForSecondsRealtime(0.3f);
        isUsingTicket = false;
    }

    public void UpdateWaveToLevelUP(int clearEnemiesWave)
    {
        waveToLevelUPTMP.text = $"WAVE TO LEVELUP: {Mathf.Max(0, 10 - clearEnemiesWave)}";
    }

    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
    }

    public void UpdateWaveUI()
    {
        currentWaveOrderInPause++;
        currentWaveTMPInPause.text = $"CURRENT WAVE: {currentWaveOrderInPause}";
    }

    public void UpdateEnemiesLevelUI()
    {
        enemiesLevel++;
        enemiesLevelTMPInPause.text = $"ENEMIES LEVEL: {enemiesLevel}";
        DataManager.SaveInt(DataManager.EnemiesLevelKey, enemiesLevel);
    }

    void OnDestroy()
    {
        spawnManager.OnWaveCompleted -= UpdateWaveUI;
        GlobalDifficultyCurveController.Instance.OnEnemiesLevelUp -= UpdateEnemiesLevelUI;
    }

    public void UpdateCountdownUI(int countdown)
    {
        // 先隐藏全部（保证只有一个显示）
        HideAllCountdownImages();

        // 只处理 1~3
        if (countdown < 1 || countdown > 3)
        {
            Debug.Log($"UpdateCountdownUI: 忽略无效倒计时值 {countdown}");
            return;
        }

        // 映射（确保 inspector 中的顺序是： index0->“3秒图”， index1->“2秒图”， index2->“1秒图”）
        int indexToShow = -1;
        switch (countdown)
        {
            case 3:
                indexToShow = 0; // 第3秒显示第0张图
                break;
            case 2:
                indexToShow = 1; // 第2秒显示第1张图
                break;
            case 1:
                indexToShow = 2; // 第1秒显示第2张图
                break;
            default:
                indexToShow = -1;
                break;
        }

        if (indexToShow >= 0)
        {
            if (countdownImages == null)
            {
                Debug.LogWarning("UpdateCountdownUI: countdownImages 为 null");
                return;
            }

            if (countdownImages.Length <= indexToShow)
            {
                Debug.LogWarning($"UpdateCountdownUI: countdownImages 长度为 {countdownImages.Length}，但需要索引 {indexToShow}");
                return;
            }

            if (countdownImages[indexToShow] == null)
            {
                Debug.LogWarning($"UpdateCountdownUI: countdownImages[{indexToShow}] 为 null");
                return;
            }

            // 激活并确保对象处于激活状态
            countdownImages[indexToShow].gameObject.SetActive(true);
        }
    }

    public void HideAllCountdownImages()
    {
        if (countdownImages == null)
        {
            Debug.LogWarning("HideAllCountdownImages: countdownImages 为 null");
            return;
        }

        for (int i = 0; i < countdownImages.Length; i++)
        {
            if (countdownImages[i] != null && countdownImages[i].gameObject.activeSelf)
            {
                countdownImages[i].gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator ShowWaveUIFade(Image uiImg, int waveOrderTMP)
    {
        if (uiImg == null || tempCurrentWaveTMP == null)
        {
            Debug.LogWarning("UI图片或文本组件未赋值！");
            _currentShowCoroutine = null;
            yield break;
        }

        tempCurrentWaveTMP.text = $"' {waveOrderTMP} '";

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
