using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 奖励管理器 - 处理道具获取、使用及冷却逻辑
/// </summary>
public class RewardManager : MonoBehaviour
{
    [Header("奖励配置")]
    [Tooltip("当前拥有的道具数量（本地持久化）")]
    public int currentPropCount; 
    
    [Tooltip("道具使用后的冷却间隔（秒）")]
    public float propUsedCooldownInterval = 15f; 

    [Tooltip("获取道具的按钮组件")]
    [SerializeField] private Button rewardButton; 
    
    [Tooltip("冷却进度遮罩图片")]
    [SerializeField] private Image cooldownMask;

    private float _cooldownTimer; // 冷却计时器
    private bool _isInCooldown;   // 是否处于冷却中

    private void Awake()
    {
        InitComponents();
    }

    private void Start()
    {
        InitPropCount();
    }

    private void Update()
    {
        UpdateCooldownLogic();
    }

    private void OnEnable()
    {
        RegisterEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    /// <summary>
    /// 初始化组件引用
    /// </summary>
    private void InitComponents()
    {
        // 自动获取按钮组件（如果未手动绑定）
        if (rewardButton == null)
        {
            Debug.LogWarning("道具获取按钮未绑定，尝试自动获取...");
            rewardButton = GetComponent<Button>();
            
            if (rewardButton == null)
            {
                Debug.LogError("自动获取按钮组件失败，请手动绑定！");
            }
        }

        // 初始化冷却遮罩
        if (cooldownMask != null)
        {
            cooldownMask.fillAmount = 0;
            cooldownMask.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("未设置冷却遮罩图片，冷却效果将无法显示");
        }
    }

    /// <summary>
    /// 初始化道具数量（从本地数据读取）
    /// </summary>
    private void InitPropCount()
    {
        currentPropCount = DataManager.GetInt(DataManager.CurrentPropCountKey, PlayerInitialConfig.CurrentPropCount);
    }

    /// <summary>
    /// 更新冷却逻辑
    /// </summary>
    private void UpdateCooldownLogic()
    {
        if (!_isInCooldown) return;

        _cooldownTimer += Time.deltaTime;
        UpdateCooldownUI();

        // 冷却结束
        if (_cooldownTimer >= propUsedCooldownInterval)
        {
            EndCooldown();
        }
    }

    /// <summary>
    /// 更新冷却UI显示
    /// </summary>
    private void UpdateCooldownUI()
    {
        if (cooldownMask == null) return;

        float fillRatio = 1 - (_cooldownTimer / propUsedCooldownInterval);
        cooldownMask.fillAmount = Mathf.Clamp01(fillRatio);
    }

    /// <summary>
    /// 结束冷却状态
    /// </summary>
    private void EndCooldown()
    {
        _isInCooldown = false;
        EnableButton();

        if (cooldownMask != null)
        {
            cooldownMask.fillAmount = 0;
            cooldownMask.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 注册事件监听
    /// </summary>
    private void RegisterEvents()
    {
        if (rewardButton != null)
        {
            rewardButton.onClick.AddListener(OnFreeRewardClicked);
        }

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnRewardedAdCompleted += OnAdRewardCompleted;
        }
        else
        {
            Debug.LogError("AdsManager未初始化，无法注册广告回调事件！");
        }
    }

    /// <summary>
    /// 注销事件监听（防止内存泄漏）
    /// </summary>
    private void UnregisterEvents()
    {
        if (rewardButton != null)
        {
            rewardButton.onClick.RemoveListener(OnFreeRewardClicked);
        }

        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnRewardedAdCompleted -= OnAdRewardCompleted;
        }
    }

    /// <summary>
    /// 免费奖励按钮点击事件
    /// </summary>
    private void OnFreeRewardClicked()
    {
        if (rewardButton == null) return;

        // 禁用按钮防止重复点击
        rewardButton.interactable = false;

        // 有道具则直接使用
        if (currentPropCount > 0)
        {
            UseProp();
            return;
        }

        // 无道具则显示激励广告
        ShowRewardedAdForReward();
    }

    /// <summary>
    /// 使用道具逻辑
    /// </summary>
    private void UseProp()
    {
        currentPropCount--;
        UpdatePropUIAndSave();
        Debug.Log("使用1个道具，剩余: " + currentPropCount);

        // 启动冷却
        StartCooldown();
    }

    /// <summary>
    /// 显示激励广告以获取奖励
    /// </summary>
    private void ShowRewardedAdForReward()
    {
        if (AdsManager.Instance == null)
        {
            Debug.LogError("AdsManager为空，无法显示广告！");
            EnableButtonDelayed(1f);
            return;
        }

        bool isAdShown = AdsManager.Instance.ShowRewardedAd();
        if (!isAdShown)
        {
            Debug.LogWarning("广告加载未完成，1秒后重新启用按钮");
            EnableButtonDelayed(1f);
        }
    }

    /// <summary>
    /// 启动冷却
    /// </summary>
    private void StartCooldown()
    {
        _isInCooldown = true;
        _cooldownTimer = 0f;

        if (cooldownMask != null)
        {
            cooldownMask.gameObject.SetActive(true);
            cooldownMask.fillAmount = 1;
        }
    }

    /// <summary>
    /// 广告奖励完成回调
    /// </summary>
    private void OnAdRewardCompleted(bool isSuccess)
    {
        EnableButton();

        if (isSuccess)
        {
            GiveReward();
        }
        else
        {
            Debug.Log("广告未完成，未获得奖励");
        }
    }

    /// <summary>
    /// 发放奖励
    /// </summary>
    private void GiveReward()
    {
        currentPropCount++;
        UpdatePropUIAndSave();
        Debug.Log("奖励发放成功，当前道具数量: " + currentPropCount);
    }

    /// <summary>
    /// 更新道具UI并保存数据
    /// </summary>
    private void UpdatePropUIAndSave()
    {
        // 更新UI显示
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowAndUpdatePropCount(currentPropCount);
        }
        else
        {
            Debug.LogError("UIManager未初始化，无法更新道具UI！");
        }

        DataManager.SaveInt(DataManager.CurrentPropCountKey, currentPropCount);
    }

    /// <summary>
    /// 启用按钮
    /// </summary>
    private void EnableButton()
    {
        if (rewardButton != null)
        {
            rewardButton.interactable = true;
        }
    }

    /// <summary>
    /// 延迟启用按钮
    /// </summary>
    private void EnableButtonDelayed(float delay)
    {
        if (rewardButton != null)
        {
            CancelInvoke(nameof(EnableButton)); // 取消可能存在的延迟调用，避免冲突
            Invoke(nameof(EnableButton), delay);
        }
    }
}