using UnityEngine;

public class FirstKillRewardManager : MonoBehaviour
{
    public static FirstKillRewardManager Instance { get; private set; }

    [Header("首杀奖励配置")]
    public int firstKillPropReward = 3; // 首杀奖励道具数量

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 是否需要显示首杀奖励面板（未领取时返回 true）
    /// </summary>
    public bool ShouldShowFKPanel(EnemyType enemyType)
    {
        string key = enemyType switch
        {
            EnemyType.Monster1 => DataManager.FirstKillMonster1Key,
            EnemyType.Monster2 => DataManager.FirstKillMonster2Key,
            _ => null
        };
        if (key == null) return false;
        return DataManager.GetInt(key, 0) == 0;
    }

    /// <summary>
    /// 发放首杀奖励（仅 UI 确认时调用）
    /// </summary>
    public void GrantFirstKillReward(EnemyType enemyType)
    {
        string key = enemyType switch
        {
            EnemyType.Monster1 => DataManager.FirstKillMonster1Key,
            EnemyType.Monster2 => DataManager.FirstKillMonster2Key,
            _ => null
        };
        if (key == null) return;

        // ✅ 幂等检查：如果已经领取过则直接返回
        if (DataManager.GetInt(key, 0) == 1)
        {
            Debug.LogWarning($"[首杀奖励] {enemyType} 已经领取过奖励，忽略重复发放。");
            return;
        }

        // 发放逻辑
        int curProp = DataManager.GetInt(DataManager.CurrentPropCountKey, 0);
        int curTicket = DataManager.GetInt(DataManager.CurrentTicketCountKey, 0);

        DataManager.SaveIntForce(DataManager.CurrentPropCountKey, curProp + firstKillPropReward);
        DataManager.SaveIntForce(DataManager.CurrentTicketCountKey, curTicket + 1);
        DataManager.SaveIntForce(key, 1); // 标记已领取

        // 同步UI显示
        var rewardMgr = FindObjectOfType<RewardManager>();
        if (rewardMgr != null)
        {
            rewardMgr.currentPropCount = DataManager.GetInt(DataManager.CurrentPropCountKey, 0);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.currentTicketCount = DataManager.GetInt(DataManager.CurrentTicketCountKey, 0);
            UIManager.Instance.ShowAndUpdateTicketCount(UIManager.Instance.currentTicketCount);

            int syncedProp = DataManager.GetInt(DataManager.CurrentPropCountKey, 0);
            UIManager.Instance.ShowAndUpdatePropCount(syncedProp);
        }

        Debug.Log($"[首杀奖励] {enemyType} 首杀完成，获得道具 +{firstKillPropReward} / 升级自选券 +1");
    }
}
