using UnityEngine;

public class Monster1 : EnemyBase
{
    new void Start()
    {
        base.Start();
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