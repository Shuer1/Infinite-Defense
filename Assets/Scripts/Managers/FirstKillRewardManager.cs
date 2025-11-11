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

        DataManager.SaveInt(DataManager.CurrentPropCountKey, curProp + firstKillPropReward);
        UIManager.Instance?.ShowAndUpdatePropCount(curProp + firstKillPropReward);

        DataManager.SaveInt(DataManager.CurrentTicketCountKey, curTicket + 1); //获得升级自选券
        UIManager.Instance?.ShowAndUpdateTicketCount(curTicket + 1);

        DataManager.SaveIntForce(key, 1); // 标记已领取

        Debug.Log($"[首杀奖励] {enemyType} 首杀完成，获得道具 +{firstKillPropReward} / 升级自选券 +1");
    }
}