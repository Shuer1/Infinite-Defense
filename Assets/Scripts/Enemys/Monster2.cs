using UnityEngine;

public class Monster2 : EnemyBase
{
    public GameObject chestPrefab2;
    new void Start()
    {
        base.Start();
        SyncEnemyData();
        currentHealth = maxHealth;
    }

    void SyncEnemyData()
    {
        enemyType = EnemyType.Monster2;
        maxHealth = DataManager.GetInt(DataManager.Monster2MaxHealthKey);
        damage = DataManager.GetInt(DataManager.Monster2DamageKey);
        expReward = DataManager.GetInt(DataManager.Monster2ExpRewardKey);
    }

    protected override void Die() // 重写死亡逻辑，增加（被）首杀检查
    {
        base.Die();

        if (FirstKillRewardManager.Instance != null && FirstKillRewardManager.Instance?.ShouldShowFKPanel(enemyType) == true)
        {
            if (chestPrefab2 != null)
            {
                GameObject chest = Instantiate(chestPrefab2, transform.position, Quaternion.Euler(0f, 180f, 0f));

                DontDestroyOnLoad(chest);

                var chestScript = chest.GetComponent<Chest>();
                if (chestScript != null)
                {
                    chestScript.enemyType = enemyType;
                }
            }
        }
    }
}