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
    /// 是否显示首杀奖励面板
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
    /// 发放首杀奖励
    /// </summary>
    /// <param name="enemyType">敌人类型（Monster1 或 Monster2）</param>
    public void GrantFirstKillReward(EnemyType enemyType)
    {
        string key = enemyType switch
        {
            EnemyType.Monster1 => DataManager.FirstKillMonster1Key,
            EnemyType.Monster2 => DataManager.FirstKillMonster2Key,
            _ => null
        };

        if (key == null) return;

        int curProp = DataManager.GetInt(DataManager.CurrentPropCountKey, 0);
        int curTicket = DataManager.GetInt(DataManager.CurrentTicketCountKey, 0);

        DataManager.SaveIntForce(DataManager.CurrentPropCountKey, curProp + firstKillPropReward); //先存
        DataManager.SaveIntForce(DataManager.CurrentTicketCountKey, curTicket + 1); //获得升级自选券
        DataManager.SaveIntForce(key, 1); // 标记已领取

        var rewardMgr = FindObjectOfType<RewardManager>();
        if (rewardMgr != null)
        {
            rewardMgr.currentPropCount = DataManager.GetInt(DataManager.CurrentPropCountKey, 0);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.currentTicketCount = DataManager.GetInt(DataManager.CurrentTicketCountKey, 0); //再取，更新
            UIManager.Instance.ShowAndUpdateTicketCount(UIManager.Instance.currentTicketCount);

            int syncedProp = DataManager.GetInt(DataManager.CurrentPropCountKey, 0);
            UIManager.Instance.ShowAndUpdatePropCount(syncedProp);

            UIManager.Instance.ShowFirstKillPanel(enemyType);
        }

        Debug.Log($"[首杀奖励] {enemyType} 首杀完成，获得道具 +{firstKillPropReward} / 升级自选券 +1");
    }
}