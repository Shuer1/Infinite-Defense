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
        DataManager.InitializeIntData(DataManager.HighScoreKey, 0);
        DataManager.InitializeIntData(DataManager.CurrentWaveKey, 1);
        DataManager.InitializeIntData(DataManager.PlayerMaxHealthKey, 100);
        DataManager.InitializeIntData(DataManager.PlayerLevelKey, 1);
        DataManager.InitializeIntData(DataManager.NextLevelExpKey, 100);
        DataManager.InitializeFloatData(DataManager.PlayerShootSpeedKey, 0.5f);
        DataManager.InitializeIntData(DataManager.CurrentPropCountKey, 3);
        DataManager.InitializeIntData(DataManager.DefenseTowerMaxHPKey, 500);
    }

    private void InitializeEnemyData()
    {
        DataManager.InitializeIntData(DataManager.EnemiesLevelKey, EnemysInitialConfig.EnemiesLevel);
        DataManager.InitializeIntData(DataManager.Enemy1MaxHealthKey, EnemysInitialConfig.Enemy1MaxHealth);
        DataManager.InitializeIntData(DataManager.Enemy2MaxHealthKey, EnemysInitialConfig.Enemy2MaxHealth);
        DataManager.InitializeIntData(DataManager.Monster1MaxHealthKey, EnemysInitialConfig.Monster1MaxHealth);
        DataManager.InitializeIntData(DataManager.Enemy1DamageKey, EnemysInitialConfig.Enemy1Damage);
        DataManager.InitializeIntData(DataManager.Enemy2DamageKey, EnemysInitialConfig.Enemy2Damage);
        DataManager.InitializeIntData(DataManager.Monster1DamageKey, EnemysInitialConfig.Monster1Damage);
        DataManager.InitializeIntData(DataManager.Enemy1ExpRewardKey, EnemysInitialConfig.Enemy1ExpReward);
        DataManager.InitializeIntData(DataManager.Enemy2ExpRewardKey, EnemysInitialConfig.Enemy2ExpReward);
        DataManager.InitializeIntData(DataManager.Monster1ExpRewardKey, EnemysInitialConfig.Monster1ExpReward);
    }

    private void InitializeBulletData()
    {
        DataManager.InitializeIntData(DataManager.BaseBulletDamageKey, 50);
        DataManager.InitializeIntData(DataManager.ExplosiveDamageKey, 5);
        DataManager.InitializeIntData(DataManager.FrostDamageKey, 5);
        DataManager.InitializeIntData(DataManager.NormalBulletChanceKey, 100);
        DataManager.InitializeIntData(DataManager.ExplosiveBulletChanceKey, 0);
        DataManager.InitializeFloatData(DataManager.ExplosionRangeKey, 1.0f);
        DataManager.InitializeFloatData(DataManager.FrostFreezeDurationKey, 1.0f);
    }
}
