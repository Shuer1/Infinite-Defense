using UnityEngine;

public class Monster1 : EnemyBase
{
    public GameObject propPrefab;
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

        if (FirstKillRewardManager.Instance != null && FirstKillRewardManager.Instance?.ShouldShowFKPanel(enemyType) == true)
        {
            UIManager.Instance?.ShowFirstKillPanel(enemyType);
        }
    }
}