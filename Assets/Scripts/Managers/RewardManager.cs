using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardManager : MonoBehaviour
{
    [Header("奖励配置")]
    public int currentPropCount; //拥有道具数量（本地保存）
    [SerializeField] private Button rewardButton; // 获取道具按钮
    private void Awake()
    {
        // 获取按钮组件
        if (rewardButton == null)
        {
            Debug.Log("道具获取按钮未绑定！尝试自动寻找添加组件！");
            rewardButton = GetComponent<Button>();
        }

    }
    
    void Start()
    {
        currentPropCount = DataManager.GetInt(DataManager.CurrentPropCountKey, PlayerInitialConfig.CurrentPropCount); //初始化道具数量
    }

    private void OnEnable()
    {
        // 注册按钮点击事件
        rewardButton.onClick.AddListener(OnFreeRewardClicked);
        // 注册广告完成回调（注意：每次启用时注册，避免重复）
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnRewardedAdCompleted += OnAdRewardCompleted;
        }
        else
        {
            Debug.LogError("AdsManager.Instance为null!未初始化或初始化未完成!");
        }

    }

    private void OnDisable()
    {
        // 取消注册，防止内存泄漏
        rewardButton.onClick.RemoveListener(OnFreeRewardClicked);
        AdsManager.Instance.OnRewardedAdCompleted -= OnAdRewardCompleted;
    }

    /// <summary>
    /// 按钮点击事件：触发激励广告
    /// </summary>
    private void OnFreeRewardClicked()
    {
        // 点击后禁用按钮，防止重复点击
        rewardButton.interactable = false;

        //道具使用逻辑，可封装成方法调用
        if (currentPropCount > 0)
        {
            currentPropCount -= 1;
            UIManager.Instance.ShowAndUpdatePropCount(currentPropCount);
            DataManager.SaveIntForce(DataManager.CurrentPropCountKey, currentPropCount);
            Debug.Log("使用道具！");
            EnableButton();
            return;
        }

        // 调用AdsManager显示激励广告
        bool isAdShown = AdsManager.Instance.ShowRewardedAd();
        if (!isAdShown)
        {
            Debug.Log("广告加载失败！等待重新启动按钮！");
            // 1秒后重新启用按钮
            Invoke(nameof(EnableButton), 1f);
        }
    }

    /// <summary>
    /// 广告奖励完成回调
    /// </summary>
    private void OnAdRewardCompleted(bool isSuccess)
    {
        // 重新启用按钮
        EnableButton();

        if (isSuccess)
        {
            // 广告成功完成：发放奖励
            GiveReward();
        }
        else
        {
            Debug.Log("未获得奖励！");
        }
    }

    /// <summary>
    /// 发放奖励（对接游戏逻辑）
    /// </summary>
    private void GiveReward()
    {
        // 1. 增加道具
        currentPropCount += 1;
        // 2. 更新显示UI
        UIManager.Instance.ShowAndUpdatePropCount(currentPropCount);
        // 3. 保存数据（获取道具为递增）
        DataManager.SaveInt(DataManager.CurrentPropCountKey,currentPropCount);
        Debug.Log("奖励发放成功！");
    }

    // 辅助方法：启用按钮
    private void EnableButton()
    {
        rewardButton.interactable = true;
    }
}