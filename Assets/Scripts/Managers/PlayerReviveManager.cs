using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerReviveManager : MonoBehaviour
{
    public static PlayerReviveManager Instance;

    [Header("可在 Inspector 预先绑定（若不绑定将在场景加载时自动查找）")]
    [SerializeField] private GameObject revivePanel;
    private const string GameOverSoundKey = "Defeat";
    [SerializeField] private Button reviveWithAdButton;
    [SerializeField] private Button gameOverButton;
    private PlayerController playerController;
    private int reviveCount = 0;
    private int maxReviveCount = 3;
    private bool allowToRevive = true;
    private bool isAdProcessing = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        // 订阅场景加载，以便在每次进入战斗场景时重新绑定
        SceneManager.sceneLoaded -= OnSceneLoaded; // 先确保未重复订阅
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // 初次尝试绑定（如果 Inspector 已配置则优先使用）
        BindSceneReferences();
        HideRevivePanel();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景切换 / 重新进入时重新绑定场景对象
        BindSceneReferences();
    }

    /// <summary>
    /// 尝试绑定场景内引用：优先使用 Inspector 填的引用，找不到则通过名称/类型查找（最小侵入）
    /// 请确保场景内的 Revive 面板/按钮 名称或 Tag 与下方查找规则一致，或在 Inspector 预先赋值。
    /// </summary>
    private void BindSceneReferences()
    {
        // ====== Bind PlayerController ======
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        // ====== Bind revivePanel ======
        if (revivePanel == null)
        {
            // 尝试按Tag查找（即使未激活也能找到）
            revivePanel = FindGameObjectWithTagIncludingInactive("RevivePanel");
            
            // 如果没有找到，则尝试按名称查找（即使未激活也能找到）
            if(revivePanel == null) 
                revivePanel = FindObjectByNameIncludingInactive("RevivePanel");
        }

        // ====== Bind Buttons（优先从 revivePanel 内查找） ======
        // reviveWithAdButton
        if (reviveWithAdButton == null && revivePanel != null)
        {
            var b = FindChildByNameIncludingInactive(revivePanel.transform, "ReviveWithAdButton");
            if (b != null) reviveWithAdButton = b.GetComponent<Button>();
        }
        if (reviveWithAdButton == null)
        {
            // 尝试全场景查找（按名，包括未激活对象）
            var found = FindObjectByNameIncludingInactive("ReviveWithAdButton");
            if (found != null) reviveWithAdButton = found.GetComponent<Button>();
        }

        // gameOverButton
        if (gameOverButton == null && revivePanel != null)
        {
            var b = FindChildByNameIncludingInactive(revivePanel.transform, "GameOverButton");
            if (b != null) gameOverButton = b.GetComponent<Button>();
        }
        if (gameOverButton == null)
        {
            var found = FindObjectByNameIncludingInactive("GameOverButton");
            if (found != null) gameOverButton = found.GetComponent<Button>();
        }

        // ====== 绑定按钮事件（先清理旧监听器以防重复） ======
        if (reviveWithAdButton != null)
        {
            reviveWithAdButton.onClick.RemoveListener(OnReviveWithAdClicked);
            reviveWithAdButton.onClick.AddListener(OnReviveWithAdClicked);
        }

        if (gameOverButton != null)
        {
            gameOverButton.onClick.RemoveListener(OnGameOverClicked);
            gameOverButton.onClick.AddListener(OnGameOverClicked);
        }

        // 若 AdsManager 在场且之前有订阅，也确保事件解绑再重订阅（避免重复）
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnReviveRewardedAdCompleted -= OnRewardedAdCompleted;
        }
    }

    /// <summary>
    /// 查找带有指定tag的对象，包括未激活的对象
    /// </summary>
    private GameObject FindGameObjectWithTagIncludingInactive(string tag)
    {
        GameObject[] objs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in objs)
        {
            if (obj.hideFlags == HideFlags.None && obj.CompareTag(tag))
                return obj;
        }
        return null;
    }

    /// <summary>
    /// 根据名称查找对象，包括未激活的对象
    /// </summary>
    private GameObject FindObjectByNameIncludingInactive(string name)
    {
        GameObject[] objs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in objs)
        {
            if (obj.hideFlags == HideFlags.None && obj.name == name)
                return obj;
        }
        return null;
    }

    /// <summary>
    /// 在父对象的子对象中根据名称查找对象，包括未激活的对象
    /// </summary>
    private GameObject FindChildByNameIncludingInactive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child.gameObject;
                
            // 递归查找子对象的子对象
            GameObject found = FindChildByNameIncludingInactive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    public void ShowRevivePanel()
    {
        if(EntireBgmManager.Instance != null)
            EntireBgmManager.Instance.PauseCurrentBgm();

        if (revivePanel != null && !GameManager.Instance.isGameOver)
        {
            revivePanel.SetActive(true);
            SoundManager.Instance.PlayEventSFX(GameOverSoundKey);
            UpdateReviveButtonStatus();
        }
        else
        {
            Debug.LogWarning("[PlayerReviveManager] 无法显示 RevivePanel（引用可能丢失或游戏已结束）");
        }
    }

    public void HideRevivePanel()
    {
        if (revivePanel != null)
            revivePanel.SetActive(false);
    }

    private void OnReviveWithAdClicked()
    {
        if (isAdProcessing || !allowToRevive || reviveCount >= maxReviveCount)
        {
            Color color = reviveWithAdButton.image.color;
            color.a = 0.2f;
            reviveWithAdButton.image.color = color;
            return;
        }
            
        isAdProcessing = true;
        Debug.Log("[PlayerReviveManager] 玩家选择通过观看广告复活");

        if (AdsManager.Instance != null)
        {
            // 防止重复订阅
            AdsManager.Instance.OnReviveRewardedAdCompleted -= OnRewardedAdCompleted;
            AdsManager.Instance.OnReviveRewardedAdCompleted += OnRewardedAdCompleted;

            if (!AdsManager.Instance.ShowReviveRewardedAd())
            {
                Debug.LogWarning("[PlayerReviveManager] 广告无法显示，直接复活");
                OnRewardedAdCompleted(true);
            }
        }
        else
        {
            Debug.LogError("[PlayerReviveManager] 未找到 AdsManager，直接复活");
            OnRewardedAdCompleted(true);
        }
    }

    private void UpdateReviveButtonStatus()
    {
        if(reviveWithAdButton == null) return;

        bool canRevive = allowToRevive && reviveCount < maxReviveCount;

        reviveWithAdButton.interactable = canRevive;

        Color btnColor = reviveWithAdButton.image.color;
        btnColor.a = canRevive ? 1f : 0.2f;
        reviveWithAdButton.image.color = btnColor;
    }

    private void OnRewardedAdCompleted(bool success)
    {
        isAdProcessing = false;

        if (AdsManager.Instance != null)
            AdsManager.Instance.OnReviveRewardedAdCompleted -= OnRewardedAdCompleted;

        if (success)
        {
            PerformRevive();
        }
        else
        {
            Debug.LogWarning("[PlayerReviveManager] 广告观看失败，无法复活");
        }
    }

    private void OnGameOverClicked()
    {
        HideRevivePanel();

        if (EntireBgmManager.Instance != null && EntireBgmManager.Instance.isPaused)
        {
            EntireBgmManager.Instance.ResumeCurrentBgm();
        }

        GameManager.Instance?.GameOver();
    }

    private void PerformRevive()
    {
        if(EntireBgmManager.Instance != null)
            EntireBgmManager.Instance.ResumeCurrentBgm();

        if (!allowToRevive || reviveCount >= maxReviveCount)
            return;

        reviveCount++;
        if (reviveCount >= maxReviveCount)
            allowToRevive = false;

        UpdateReviveButtonStatus();
        HideRevivePanel();

        // 确保引用存在，若不存在则尝试重找一次
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (playerController != null)
            playerController.ResetLive();
        else
            Debug.LogError("[PlayerReviveManager] 未找到 PlayerController（复活后无法重置玩家）");

        if (DefenseTowerController.Instance != null)
            DefenseTowerController.Instance.ResumeTower();
        else
            Debug.LogError("[PlayerReviveManager] 未找到 DefenseTowerController（复活后无法恢复塔）");

        var enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager != null)
            foreach (var enemy in enemyManager.activeEnemies)
                enemy?.ResetEnemyState(enemy.transform.position, enemy.transform.rotation);

        GameManager.Instance.isGameOver = false;
        Debug.Log("[PlayerReviveManager] 玩家与防御塔复活成功，继续战斗");
    }

    public void OnPlayerDied()
    {
        // 进入死亡流程时再次确保引用（防止引用丢失导致无法显示）
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        BindSceneReferences(); // 再次保证 UI 已绑定
        ShowRevivePanel();
    }

    private void OnDisable()
    {
        // 取消场景加载订阅
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // 解绑按钮事件
        if (reviveWithAdButton != null)
            reviveWithAdButton.onClick.RemoveListener(OnReviveWithAdClicked);
        if (gameOverButton != null)
            gameOverButton.onClick.RemoveListener(OnGameOverClicked);

        // 解绑 Ads 回调
        if (AdsManager.Instance != null)
            AdsManager.Instance.OnReviveRewardedAdCompleted -= OnRewardedAdCompleted;
    }

    private void OnDestroy()
    {
        // 同 OnDisable 的清理，冗余保障
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (reviveWithAdButton != null)
            reviveWithAdButton.onClick.RemoveListener(OnReviveWithAdClicked);
        if (gameOverButton != null)
            gameOverButton.onClick.RemoveListener(OnGameOverClicked);

        if (AdsManager.Instance != null)
            AdsManager.Instance.OnReviveRewardedAdCompleted -= OnRewardedAdCompleted;
    }
}