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
    /// 检查并发放首杀奖励
    /// </summary>
    /// <param name="enemyType">敌人类型（Monster1 或 Monster2）</param>
    public void TryGrantFirstKillReward(EnemyType enemyType)
    {
        string key = enemyType switch
        {
            EnemyType.Monster1 => DataManager.FirstKillMonster1Key,
            EnemyType.Monster2 => DataManager.FirstKillMonster2Key,
            _ => null
        };

        if (key == null) return;

        int alreadyKilled = DataManager.GetInt(key, 0);
        if (alreadyKilled == 1) return; // 已领取过

        // 标记已领取
        DataManager.SaveInt(key, 1);

        // 发放奖励
        int current = DataManager.GetInt(DataManager.CurrentPropCountKey, 0);
        DataManager.SaveIntForce(DataManager.CurrentPropCountKey, current + firstKillPropReward);

        // UI更新
        UIManager.Instance?.ShowAndUpdatePropCount(current + firstKillPropReward);

        Debug.Log($"[首杀奖励] {enemyType} 首杀完成，获得道具 +{firstKillPropReward}");
    }
}