using UnityEngine;

public class Monster2 : EnemyBase
{
    new void Start()
    {
        base.Start();
        SyncEnemyData();
        currentHealth = maxHealth;
    }

    void SyncEnemyData()
    {
        maxHealth = DataManager.GetInt(DataManager.Monster2MaxHealthKey);
        damage = DataManager.GetInt(DataManager.Monster2DamageKey);
        expReward = DataManager.GetInt(DataManager.Monster2ExpRewardKey);
    }
}