using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;

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
    
    // 添加炸弹位置和范围的字段
    private Vector3 bombPosition;
    private float bombRangeSaved;

    // 存储延迟执行的动作
    private Dictionary<string, Action> delayedActions = new Dictionary<string, Action>();

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
            // Initialize with a default size, will be adjusted dynamically
            bombRangeIndicator.sizeDelta = new Vector2(100, 100);
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
            UpdateIndicatorSize();
        }
    }

    private void OnPropDragging(BaseEventData data)
    {
        if (!_isDraggingProp) return;

        var pointerData = (PointerEventData)data;
        Vector3 targetPos = pointerData.position;

        // 平滑插值指示器位置（跟随鼠标/手指）
        if (bombRangeIndicator != null)
        {
            //Vector3 targetPos = pointerData.position;
            bombRangeIndicator.position = Vector3.Lerp(bombRangeIndicator.position, targetPos, 0.3f);
            UpdateIndicatorSize();
        }
    }

    public void CancelDraggingPropIfAny()
    {
        if (!_isDraggingProp) return;

        _isDraggingProp = false;

        if (bombRangeIndicator != null)
            bombRangeIndicator.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Updates the bomb range indicator size to match the actual bomb range in world space
    /// </summary>
    private void UpdateIndicatorSize()
    {
        if (bombRangeIndicator == null || uiCamera == null) return;
        
        // Calculate world position at a standard distance from camera
        Vector3 testWorldPos = new Vector3(0, 0, 10); // Point 10 units away from camera
        
        // Convert two world points to screen positions to determine scale
        Vector3 screenPoint1 = uiCamera.WorldToScreenPoint(testWorldPos);
        Vector3 screenPoint2 = uiCamera.WorldToScreenPoint(testWorldPos + new Vector3(bombRange, 0, 0));
        
        // Calculate the screen distance that represents the bomb range
        float screenRange = Vector3.Distance(screenPoint1, screenPoint2);
        
        // Apply the size (diameter = radius * 2)
        bombRangeIndicator.sizeDelta = new Vector2(screenRange * 2, screenRange * 2);
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

        bombPosition = position;
        bombRangeSaved = range;

        // ✅ 播放爆炸特效
        if (bombEffectPrefab != null)
        {
            GameObject fx = Instantiate(bombEffectPrefab, position, Quaternion.identity);

            //float longestDuration = 0f;
            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.useUnscaledTime = true;
            }

            float longestDuration = fx.GetComponentsInChildren<ParticleSystem>().Max(p => p.main.duration);
            StartCoroutine(TriggerBombDamageUnscaled(longestDuration, fx));
        }
        else
        {
            // 如果没有特效，则立即触发伤害
            TriggerBombDamageAtPosition(position, range);
        }
    }
    
    private IEnumerator TriggerBombDamageUnscaled(float delay, GameObject fx)
    {
        float timer = 0f;
        while (timer < delay)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        TriggerBombDamageAtPosition(bombPosition, bombRangeSaved);

        Destroy(fx,0.5f);
    }
    
    private void TriggerBombDamage()
    {
        TriggerBombDamageAtPosition(bombPosition, bombRangeSaved);
    }
    
    private void TriggerBombDamageAtPosition(Vector3 position, float range)
    {
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

    /// <summary>
    /// 带参数的Invoke调用
    /// </summary>
    /// <param name="methodName">方法名称（用于标识）</param>
    /// <param name="action">要执行的操作</param>
    /// <param name="delay">延迟时间</param>
    private void InvokeWithParameters(string methodName, Action action, float delay)
    {
        // 将动作存储在字典中
        delayedActions[methodName] = action;
        
        // 创建一个包装方法来调用存储的动作
        System.Action wrapper = () => {
            if (delayedActions.ContainsKey(methodName)) {
                delayedActions[methodName]?.Invoke();
                delayedActions.Remove(methodName);
            }
        };
        
        // 使用Invoke调用包装方法
        Invoke(nameof(ExecuteDelayedAction), delay);
    }
    
    /// <summary>
    /// 执行延迟动作的中间方法
    /// </summary>
    private void ExecuteDelayedAction()
    {
        // 这是一个占位方法，实际不会被直接调用
        // 真正的调用会通过InvokeWithParameters创建的包装器进行
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
