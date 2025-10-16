using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// 奖励管理器 - 处理道具获取、使用及冷却逻辑（支持拖动释放）
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

    private float _cooldownTimer;
    private bool _isInCooldown;

    [Header("道具拖动释放配置")]
    [Tooltip("爆炸范围指示器(圆形UI,设置为Filled Mode)")]
    [SerializeField] private RectTransform bombRangeIndicator;
    [Tooltip("道具爆炸属性")]
    [SerializeField] private int ult_Damage = 200;
    [SerializeField] private float bombRange = 5f;
    [SerializeField] private GameObject bombEffectPrefab;

    [Tooltip("UI相机(默认使用MainCamera)")]
    [SerializeField] private Camera uiCamera;

    private bool _isDraggingProp;
    private Vector2 _dragStartPos;
    private const float _dragThreshold = 100f;

    private void Awake() => InitComponents();
    private void Start() => InitPropCount();
    private void Update() => UpdateCooldownLogic();

    private void OnEnable() => RegisterEvents();
    private void OnDisable() => UnregisterEvents();

    private void InitComponents()
    {
        if (rewardButton == null)
        {
            rewardButton = GetComponent<Button>();
            if (rewardButton == null)
                Debug.LogError("Reward button 未绑定！");
        }

        if (cooldownMask != null)
        {
            cooldownMask.fillAmount = 0;
            cooldownMask.gameObject.SetActive(false);
        }

        InitDragComponents();
    }

    private void InitDragComponents()
    {
        if (bombRangeIndicator != null)
        {
            bombRangeIndicator.gameObject.SetActive(false);
            bombRangeIndicator.sizeDelta = new Vector2(bombRange * 2, bombRange * 2);
        }

        if (uiCamera == null)
            uiCamera = Camera.main ?? FindObjectOfType<Camera>();

        // 添加拖动事件系统
        if (rewardButton != null)
        {
            var trigger = rewardButton.GetComponent<EventTrigger>() ?? rewardButton.gameObject.AddComponent<EventTrigger>();
            AddDragEvent(trigger, EventTriggerType.PointerDown, OnPropPointerDown);
            AddDragEvent(trigger, EventTriggerType.Drag, OnPropDragging);
            AddDragEvent(trigger, EventTriggerType.PointerUp, OnPropPointerUp);
        }
    }

    private void AddDragEvent(EventTrigger trigger, EventTriggerType type, UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }

    private void OnPropPointerDown(BaseEventData data)
    {
        if (currentPropCount <= 0 || _isInCooldown) return;

        var pointerData = (PointerEventData)data;
        _dragStartPos = pointerData.position;
        _isDraggingProp = true;

        if (bombRangeIndicator != null)
        {
            bombRangeIndicator.gameObject.SetActive(true);
            bombRangeIndicator.position = pointerData.position;
        }
    }

    private void OnPropDragging(BaseEventData data)
    {
        if (!_isDraggingProp) return;

        var pointerData = (PointerEventData)data;

        // 平滑插值指示器位置（跟随鼠标/手指）
        if (bombRangeIndicator != null)
        {
            Vector3 targetPos = pointerData.position;
            bombRangeIndicator.position = Vector3.Lerp(bombRangeIndicator.position, targetPos, 0.3f);
        }
    }

    private void OnPropPointerUp(BaseEventData data)
    {
        if (!_isDraggingProp) return;

        var pointerData = (PointerEventData)data;
        float dragDistance = Vector2.Distance(_dragStartPos, pointerData.position);

        if (dragDistance >= _dragThreshold)
        {
            Vector3 worldPos = ScreenToWorldPoint(pointerData.position);
            ULT_Bomb(worldPos, bombRange);

            UseProp();
        }

        _isDraggingProp = false;
        if (bombRangeIndicator != null)
            bombRangeIndicator.gameObject.SetActive(false);
    }

    private Vector3 ScreenToWorldPoint(Vector2 screenPos)
    {
        if (uiCamera == null) return Vector3.zero;

        Ray ray = uiCamera.ScreenPointToRay(screenPos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }
        return Vector3.zero;
    }

    private void ULT_Bomb(Vector3 position, float range)
    {
        Debug.Log($"导弹释放：位置={position}，范围={range}");
        // ✅ 1. 可选：播放爆炸特效
        if (bombEffectPrefab != null)
        {
            GameObject fx = Instantiate(bombEffectPrefab, position, Quaternion.identity);
            Destroy(fx, 3f); // 自动销毁特效对象
        }

        // ✅ 2. 检测范围内敌人（3D物理）
        Collider[] targets = Physics.OverlapSphere(position, range);

        int hitCount = 0;

        foreach (var target in targets)
        {
            if (target.CompareTag("Enemy"))
            {
                EnemyBase enemy = target.GetComponent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeDamage(ult_Damage);
                    hitCount++;
                }
            }
        }

        Debug.Log($"💥 爆炸命中敌人数量: {hitCount}，造成伤害: {ult_Damage}");
    }

    private void InitPropCount() =>
        currentPropCount = DataManager.GetInt(DataManager.CurrentPropCountKey, PlayerInitialConfig.CurrentPropCount);

    private void UpdateCooldownLogic()
    {
        if (!_isInCooldown) return;

        _cooldownTimer += Time.deltaTime;
        UpdateCooldownUI();

        if (_cooldownTimer >= propUsedCooldownInterval)
            EndCooldown();
    }

    private void UpdateCooldownUI()
    {
        if (cooldownMask == null) return;
        float fillRatio = 1 - (_cooldownTimer / propUsedCooldownInterval);
        cooldownMask.fillAmount = Mathf.Clamp01(fillRatio);
    }

    private void StartCooldown()
    {
        _isInCooldown = true;
        _cooldownTimer = 0f;

        // ✅ 禁用按钮交互，禁止在冷却期间再次点击或拖动
        if (rewardButton != null)
            rewardButton.interactable = false;

        if (cooldownMask != null)
        {
            cooldownMask.gameObject.SetActive(true);
            cooldownMask.fillAmount = 1;
        }
    }

    private void EndCooldown()
    {
        _isInCooldown = false;

        // ✅ 冷却结束重新启用按钮交互
        EnableButton();

        if (cooldownMask != null)
        {
            cooldownMask.fillAmount = 0;
            cooldownMask.gameObject.SetActive(false);
        }
    }

    private void RegisterEvents()
    {
        if (rewardButton != null)
            rewardButton.onClick.AddListener(OnFreeRewardClicked);

        if (AdsManager.Instance != null)
            AdsManager.Instance.OnRewardedAdCompleted += OnAdRewardCompleted;
    }

    private void UnregisterEvents()
    {
        if (rewardButton != null)
            rewardButton.onClick.RemoveListener(OnFreeRewardClicked);

        if (AdsManager.Instance != null)
            AdsManager.Instance.OnRewardedAdCompleted -= OnAdRewardCompleted;
    }

    private void OnFreeRewardClicked()
    {
        if (_isDraggingProp || _isInCooldown || rewardButton == null || currentPropCount > 0) return;

        rewardButton.interactable = false;

        ShowRewardedAdForReward();
    }

    private void UseProp()
    {
        currentPropCount--;
        UpdatePropUIAndSave();
        StartCooldown();
    }

    private void ShowRewardedAdForReward()
    {
        if (AdsManager.Instance == null)
        {
            EnableButtonDelayed(1f);
            return;
        }

        bool shown = AdsManager.Instance.ShowRewardedAd();
        if (!shown)
            EnableButtonDelayed(1f);
    }

    private void OnAdRewardCompleted(bool success)
    {
        // ✅ 若仍在冷却中，不恢复交互
        if (!_isInCooldown)
            EnableButton();

        if (success)
            GiveReward();
    }

    private void GiveReward()
    {
        // ✅ 若处于冷却，不允许获得新的道具
        if (_isInCooldown) return;

        currentPropCount++;
        UpdatePropUIAndSave();
    }

    private void UpdatePropUIAndSave()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowAndUpdatePropCount(currentPropCount);

        DataManager.SaveIntForce(DataManager.CurrentPropCountKey, currentPropCount);
    }

    private void EnableButton()
    {
        if (rewardButton != null)
            rewardButton.interactable = true;
    }

    private void EnableButtonDelayed(float delay)
    {
        if (rewardButton == null) return;
        CancelInvoke(nameof(EnableButton));
        Invoke(nameof(EnableButton), delay);
    }
}
