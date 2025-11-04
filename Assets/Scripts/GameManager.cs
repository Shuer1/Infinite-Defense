using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int score = 0;
    public bool isGameOver;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePlayerData();
        InitializeEnemyData();
        InitializeBulletData();

        DataManager.FlushSave();
        Debug.Log("[GameManager] 所有数据初始化完成");
    }
    public void AddScore(int value)
    {
        score += value;
        UIManager.Instance.UpdateScore(score);
        DataManager.SaveInt(DataManager.HighScoreKey, score);
    }

    public void GameOver()
    {
        if (isGameOver) return; //避免重复触发

        isGameOver = true;
        DataManager.FlushSave();
        UIManager.Instance.ShowGameOver();
    }

    public void Restart()
    {
        isGameOver = false;
    }

    private void InitializePlayerData()
    {
        DataManager.InitializeIntData(DataManager.HighScoreKey, PlayerInitialConfig.HighScore);
        DataManager.InitializeIntData(DataManager.CurrentWaveKey, PlayerInitialConfig.CurrentWave);
        DataManager.InitializeIntData(DataManager.PlayerMaxHealthKey, PlayerInitialConfig.MaxHealth);
        DataManager.InitializeIntData(DataManager.PlayerLevelKey,  PlayerInitialConfig.Level);
        DataManager.InitializeIntData(DataManager.NextLevelExpKey, PlayerInitialConfig.NextLevelExp);
        DataManager.InitializeFloatData(DataManager.PlayerShootSpeedKey, PlayerInitialConfig.ShootInterval);
        DataManager.InitializeIntData(DataManager.CurrentPropCountKey, PlayerInitialConfig.CurrentPropCount);
        // tower health
        DataManager.InitializeIntData(DataManager.DefenseTowerMaxHPKey, 500);
    }

    private void InitializeEnemyData()
    {
        DataManager.InitializeIntData(DataManager.EnemiesLevelKey, EnemysInitialConfig.EnemiesLevel);
        // enemies health
        DataManager.InitializeIntData(DataManager.Enemy1MaxHealthKey, EnemysInitialConfig.Enemy1MaxHealth);
        DataManager.InitializeIntData(DataManager.Enemy2MaxHealthKey, EnemysInitialConfig.Enemy2MaxHealth);
        DataManager.InitializeIntData(DataManager.Monster1MaxHealthKey, EnemysInitialConfig.Monster1MaxHealth);
        // enemies damage
        DataManager.InitializeIntData(DataManager.Enemy1DamageKey, EnemysInitialConfig.Enemy1Damage);
        DataManager.InitializeIntData(DataManager.Enemy2DamageKey, EnemysInitialConfig.Enemy2Damage);
        DataManager.InitializeIntData(DataManager.Monster1DamageKey, EnemysInitialConfig.Monster1Damage);
        // enemies exp
        DataManager.InitializeIntData(DataManager.Enemy1ExpRewardKey, EnemysInitialConfig.Enemy1ExpReward);
        DataManager.InitializeIntData(DataManager.Enemy2ExpRewardKey, EnemysInitialConfig.Enemy2ExpReward);
        DataManager.InitializeIntData(DataManager.Monster1ExpRewardKey, EnemysInitialConfig.Monster1ExpReward);
    }

    private void InitializeBulletData()
    {
        // base
        DataManager.InitializeIntData(DataManager.BaseBulletDamageKey, BulletsInitialConfig.BasicBulletDamage);
        DataManager.InitializeIntData(DataManager.NormalBulletChanceKey, BulletsChanceConfig.NormalBulletChance);
        // explosive
        DataManager.InitializeIntData(DataManager.ExplosiveDamageKey, BulletsInitialConfig.ExplosiveBulletDamage);
        DataManager.InitializeIntData(DataManager.ExplosiveBulletChanceKey, BulletsChanceConfig.ExplosiveBulletChance);
        DataManager.InitializeFloatData(DataManager.ExplosionRangeKey, BulletsInitialConfig.ExplosionRange);
        // frost
        DataManager.InitializeIntData(DataManager.FrostDamageKey, BulletsInitialConfig.FrostBulletDamage);
        DataManager.InitializeIntData(DataManager.FrostBulletChanceKey, BulletsChanceConfig.FrostBulletChance);
        DataManager.InitializeFloatData(DataManager.FrostFreezeDurationKey, BulletsInitialConfig.FrostFreezeDuration);
        // lightning
        DataManager.InitializeIntData(DataManager.LightningBulletDamageKey, BulletsInitialConfig.LightningBulletDamage);
        DataManager.InitializeIntData(DataManager.LightningBulletChanceKey, BulletsChanceConfig.LightningBulletChance);
        DataManager.InitializeIntData(DataManager.LightningCountKey, BulletsInitialConfig.LightningCount);
    }
}
