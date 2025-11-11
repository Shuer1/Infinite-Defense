using UnityEngine;

public class EnemyHeavy : EnemyBase
{
    new void Start()
    {
        base.Start();
        SyncEnemyData();
        currentHealth = maxHealth;
    }

    void SyncEnemyData()
    {
        enemyType = EnemyType.Heavy;
        maxHealth = DataManager.GetInt(DataManager.Enemy2MaxHealthKey);
        damage = DataManager.GetInt(DataManager.Enemy2DamageKey);
        expReward = DataManager.GetInt(DataManager.Enemy2ExpRewardKey);
    }
}
