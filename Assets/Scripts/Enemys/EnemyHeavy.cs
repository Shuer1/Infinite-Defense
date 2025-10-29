using UnityEngine;

public class EnemyHeavy : EnemyBase
{
    new void Start()
    {
        base.Start();
        SyncEnemyData();
    }

    void SyncEnemyData()
    {
        maxHealth = DataManager.GetInt(DataManager.Enemy2MaxHealthKey);
        currentHealth = maxHealth;
        damage = DataManager.GetInt(DataManager.Enemy2DamageKey);
    }
}
