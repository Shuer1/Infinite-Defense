using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int score = 0;
    public bool isGameOver;
    [SerializeField] private GameObject upgradePanel;

    void Awake()
    {
        if (Instance == null) Instance = this;

        upgradePanel?.SetActive(true);

        InitializePlayerData();
        InitializeEnemyData();
        InitializeBulletData();

        DataManager.FlushSave();
        Debug.Log("[GameManager]所有数据初始化完成,已统一保存");
    }

    public void AddScore(int value)
    {
        score += value;
        UIManager.Instance.UpdateScore(score);

        DataManager.SaveInt(DataManager.HighScoreKey, score);
    }

    public void GameOver()
    {
        DataManager.FlushSave();
        UIManager.Instance.ShowGameOver();
    }

    public void Restart()
    {
        isGameOver = false;
    }
    
    // 初始化玩家相关数据
    private void InitializePlayerData()
    {
        DataManager.InitializeIntData(DataManager.HighScoreKey, 0);
        DataManager.InitializeIntData(DataManager.CurrentWaveKey, 1);
        DataManager.InitializeIntData(DataManager.PlayerMaxHealthKey, 100);
        DataManager.InitializeIntData(DataManager.PlayerLevelKey, 1);
        DataManager.InitializeIntData(DataManager.NextLevelExpKey, 100);
        DataManager.InitializeFloatData(DataManager.PlayerShootSpeedKey, 0.5f);
        DataManager.InitializeIntData(DataManager.CurrentPropCountKey, 3);

        // 新增：初始化城墙血量数据⚠️
        DataManager.InitializeIntData(DataManager.DefenseTowerMaxHPKey,500);
    }

    // 初始化敌人相关数据
    private void InitializeEnemyData()
    {
        DataManager.InitializeIntData(DataManager.Enemy1MaxHealthKey, 150);
        DataManager.InitializeIntData(DataManager.Enemy2MaxHealthKey, 200);
        DataManager.InitializeIntData(DataManager.Enemy1DamageKey, 20);
        DataManager.InitializeIntData(DataManager.Enemy2DamageKey, 35);
    }

    // 初始化子弹相关数据
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
