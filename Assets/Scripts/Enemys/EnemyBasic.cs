using UnityEngine;

public class EnemyBasic : EnemyBase
{
    new void Start()
    {
        base.Start(); // 调用基类初始化
        SyncEnemyData();
        currentHealth = maxHealth;
    }

    void SyncEnemyData()
    {
        maxHealth = DataManager.GetInt(DataManager.Enemy1MaxHealthKey);
        damage = DataManager.GetInt(DataManager.Enemy1DamageKey);
        expReward = DataManager.GetInt(DataManager.Enemy1ExpRewardKey);
    }
}
