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
        enemyType = EnemyType.Monster1;
        maxHealth = DataManager.GetInt(DataManager.Monster1MaxHealthKey);
        damage = DataManager.GetInt(DataManager.Monster1DamageKey);
        expReward = DataManager.GetInt(DataManager.Monster1ExpRewardKey);
    }

    protected override void Die() // Override logic of Die function. Add the detection of first killed.
    {
        base.Die();
        FirstKillRewardManager.Instance?.TryGrantFirstKillReward(enemyType);
    }
}