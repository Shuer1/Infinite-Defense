using UnityEngine;

public class Monster1 : EnemyBase
{
    public GameObject chestPrefab1;
    private const string KillMonsterKey = "KillMonster";
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

        if (FirstKillRewardManager.Instance != null && FirstKillRewardManager.Instance?.ShouldShowFKPanel(enemyType) == true) //控制是否生成宝箱
        {
            if (chestPrefab1 != null)
            {
                GameObject chest = Instantiate(chestPrefab1, transform.position, Quaternion.Euler(0f, 180f, 0f));

                DontDestroyOnLoad(chest);

                var chestScript = chest.GetComponent<Chest>();
                if (chestScript != null)
                {
                    chestScript.enemyType = enemyType;
                }

                SoundManager.Instance.PlayEventSFX(KillMonsterKey);
            }
        }
    }
}