using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerReviveManager : MonoBehaviour
{
    public static PlayerReviveManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject revivePanel;
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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (reviveWithAdButton != null)
            reviveWithAdButton.onClick.AddListener(OnReviveWithAdClicked);

        if (gameOverButton != null)
            gameOverButton.onClick.AddListener(OnGameOverClicked);

        HideRevivePanel();
    }

    public void ShowRevivePanel()
    {
        if (revivePanel != null && !GameManager.Instance.isGameOver)
            revivePanel.SetActive(true);
    }

    public void HideRevivePanel()
    {
        if (revivePanel != null)
            revivePanel.SetActive(false);
    }

    private void OnReviveWithAdClicked()
    {
        if (isAdProcessing || !allowToRevive || reviveCount >= maxReviveCount)
            return;

        isAdProcessing = true;
        Debug.Log("[PlayerReviveManager] 玩家选择通过观看广告复活");

        if (AdsManager.Instance != null)
        {
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
            Debug.LogError("[PlayerReviveManager] 未找到AdsManager，直接复活");
            OnRewardedAdCompleted(true);
        }
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
        GameManager.Instance?.GameOver();
    }

    private void PerformRevive()
    {
        if (!allowToRevive || reviveCount >= maxReviveCount)
            return;

        reviveCount++;
        if (reviveCount >= maxReviveCount)
            allowToRevive = false;

        HideRevivePanel();

        if (playerController != null)
            playerController.ResetLive();
        else
            Debug.LogError("[PlayerReviveManager] 未找到PlayerController");

        if (DefenseTowerController.Instance != null)
            DefenseTowerController.Instance.ResumeTower();
        else
            Debug.LogError("[PlayerReviveManager] 未找到DefenseTowerController");

        var enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager != null)
            foreach (var enemy in enemyManager.activeEnemies)
                enemy?.ResetEnemyState(enemy.transform.position, enemy.transform.rotation);

        // ✅ 确保复活后恢复正常游戏状态
        GameManager.Instance.isGameOver = false;

        Debug.Log("[PlayerReviveManager] 玩家与防御塔复活成功，继续战斗");
    }

    public void OnPlayerDied()
    {
        ShowRevivePanel();
    }

    private void OnDestroy()
    {
        if (AdsManager.Instance != null)
            AdsManager.Instance.OnReviveRewardedAdCompleted -= OnRewardedAdCompleted;
    }
}
