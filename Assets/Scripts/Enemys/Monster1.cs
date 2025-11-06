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
        maxHealth = DataManager.GetInt(DataManager.Monster1MaxHealthKey);
        damage = DataManager.GetInt(DataManager.Monster1DamageKey);
        expReward = DataManager.GetInt(DataManager.Monster1ExpRewardKey);
    }
}