using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerReviveManager : MonoBehaviour
{
    public static PlayerReviveManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject revivePanel;
    [SerializeField] private Button reviveWithAdButton;
    //[SerializeField] private Button reviveWithCoinsButton;
    [SerializeField] private Button gameOverButton;
    /*
    [Header("复活消耗")]
    [SerializeField] private int coinCostForRevive = 100;
    */

    private PlayerController playerController;
    private bool hasRevived = false;

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
        playerController = FindObjectOfType<PlayerController>();

        if (reviveWithAdButton != null)
        {
            reviveWithAdButton.onClick.AddListener(OnReviveWithAdClicked);
        }

        /*
        if (reviveWithCoinsButton != null)
        {
            reviveWithCoinsButton.onClick.AddListener(OnReviveWithCoinsClicked);
        }
        */

        if (gameOverButton != null)
        {
            gameOverButton.onClick.AddListener(OnGameOverClicked);
        }

        HideRevivePanel();
    }

    public void ShowRevivePanel()
    {
        if (revivePanel != null)
        {
            revivePanel.SetActive(true);
        }
    }

    public void HideRevivePanel()
    {
        if (revivePanel != null)
        {
            revivePanel.SetActive(false);
        }
    }

    private void OnReviveWithAdClicked()
    {
        Debug.Log("[PlayerReviveManager] 玩家选择通过观看广告复活");
        
        // 检查是否有可用的激励广告
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnRewardedAdCompleted += OnRewardedAdCompleted;
            bool adShown = AdsManager.Instance.ShowRewardedAd();
            
            if (!adShown)
            {
                Debug.LogWarning("[PlayerReviveManager] 无法显示激励广告，使用备选复活方案");
                PerformRevive();
                AdsManager.Instance.OnRewardedAdCompleted -= OnRewardedAdCompleted;
            }
        }
        else
        {
            Debug.LogError("[PlayerReviveManager] AdsManager实例不存在，无法播放激励广告");
            PerformRevive(); // 如果没有广告系统，默认复活
        }
    }

    private void OnRewardedAdCompleted(bool success)
    {
        Debug.Log($"[PlayerReviveManager] 激励广告完成，success={success}");
        
        // 取消监听事件
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnRewardedAdCompleted -= OnRewardedAdCompleted;
        }

        if (success)
        {
            PerformRevive();
        }
        else
        {
            Debug.LogWarning("[PlayerReviveManager] 激励广告观看失败，不执行复活");
            // 可以在这里添加提示，比如"广告观看失败，请重试"等
        }
    }

    private void OnReviveWithCoinsClicked()
    {
        Debug.Log("[PlayerReviveManager] 玩家选择通过金币复活");
        
        // 这里应该检查玩家是否有足够的金币
        // 暂时跳过检查逻辑，直接复活
        PerformRevive();
    }

    private void OnGameOverClicked()
    {
        Debug.Log("[PlayerReviveManager] 玩家选择游戏结束");
        
        HideRevivePanel();
        
        // 调用游戏结束逻辑
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        else
        {
            UIManager.Instance?.ShowGameOver();
        }
    }

    private void PerformRevive()
    {
        if (hasRevived)
        {
            Debug.LogWarning("[PlayerReviveManager] 玩家已经复活过一次，不能再次复活");
            return;
        }

        hasRevived = true;
        HideRevivePanel();

        // 执行复活逻辑
        if (playerController != null)
        {
            playerController.ResetLive();
            Debug.Log("[PlayerReviveManager] 玩家成功复活");
        }
        else
        {
            Debug.LogError("[PlayerReviveManager] 无法找到PlayerController，复活失败");
        }
    }

    // 当玩家死亡时调用此方法
    public void OnPlayerDied()
    {
        hasRevived = false; // 重置复活标记
        
        // 显示复活面板
        ShowRevivePanel();
    }
}