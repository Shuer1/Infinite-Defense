using UnityEngine;

public class GlobalDifficultyCurveController : MonoBehaviour
{
    public static GlobalDifficultyCurveController Instance;

    public EnemyManager eM;

    public event System.Action OnEnemiesLevelUp;
    public event System.Action OnRewardLevelStarted;
    public event System.Action OnRewardLevelEnded;

    private int clearEnemyWaveCount = 0;
    private int currentWave = 1;

    // ---------------- 奖励关配置 ----------------
    [Header("奖励关卡配置")]
    public float rewardLevelBaseChance = 0.05f;     // 基础概率
    public float rewardWaveIncrement = 0.05f;       // 每波 +0.05
    public float rewardFailIncrement = 0.1f;        // 玩家失败 +0.1
    public float rewardLevelMaxChance = 0.5f;       // 概率上限
    public float rewardLevelStatMultiplier = 0.7f;  // 属性衰减
    public float rewardLevelExpMultiplier = 1.5f;   // 经验增幅

    private const string RewardChanceKey = "RewardLevelCurrentChance";

    private float currentRewardChance;
    private bool isRewardLevel = false;

    // ---------------- 敌人升级固定提升参数 ----------------
    private const int upgradeThreshold = 10;

    private int baseEnemyMaxHPDelta = 50;
    private int baseEnemyDmgDelta = 10;
    private int baseEnemyExpDelta = 5;

    private int heavyEnemyMaxHPDelta = 100;
    private int heavyEnemyDmgDelta = 15;
    private int heavyEnemyExpDelta = 15;

    private int monster1HPDelta = 350;
    private int monster1DmgDelta = 25;
    private int monster1ExpDelta = 65;

    private int monster2HPDelta = 500;
    private int monster2DmgDelta = 35;
    private int monster2ExpDelta = 80;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SyncData();

        if (eM == null)
            eM = FindObjectOfType<EnemyManager>();

        if (eM != null)
            eM.OnAllEnemiesCleared += OnWaveCleared;
    }

    private void SyncData()
    {
        clearEnemyWaveCount = DataManager.GetInt(DataManager.ClearEnemiesCountKey, 0);
        currentWave = DataManager.GetInt(DataManager.CurrentWaveKey, 1);

        // 读取持久化概率
        currentRewardChance = DataManager.GetFloat(RewardChanceKey, rewardLevelBaseChance);
        currentRewardChance = Mathf.Clamp(currentRewardChance, 0f, rewardLevelMaxChance);
    }

    // ---------------- 奖励关概率接口（外部也能调用） ----------------
    public float GetRewardChance() => currentRewardChance;

    public void SetRewardChance(float v)
    {
        currentRewardChance = Mathf.Clamp(v, 0f, rewardLevelMaxChance);
        DataManager.SaveFloatForce(RewardChanceKey, currentRewardChance);
    }

    public void IncreaseRewardChance(float delta)
    {
        SetRewardChance(currentRewardChance + delta);
    }

    // ---------------- 每波清空敌人触发 ----------------
    private void OnWaveCleared()
    {
        if (isRewardLevel)
        {
            EndRewardLevel();
        }

        clearEnemyWaveCount++;
        currentWave++;

        UIManager.Instance?.UpdateWaveToLevelUP(clearEnemyWaveCount);
        DataManager.SaveIntForce(DataManager.CurrentWaveKey, currentWave);
        DataManager.SaveIntForce(DataManager.ClearEnemiesCountKey, clearEnemyWaveCount);

        // 每波固定增加奖励关概率
        IncreaseRewardChance(rewardWaveIncrement);

        TryTriggerRewardLevel();

        if (clearEnemyWaveCount >= upgradeThreshold)
        {
            clearEnemyWaveCount = 0;
            DataManager.SaveIntForce(DataManager.ClearEnemiesCountKey, 0);
            UpgradeEnemies();
        }
    }

    // ---------------- 敌人升级 ----------------
    private void UpgradeEnemies()
    {
        int lvl = DataManager.GetInt(DataManager.EnemiesLevelKey, 1);
        DataManager.SaveIntForce(DataManager.EnemiesLevelKey, lvl + 1);

        DataManager.SaveIntForce(DataManager.Enemy1MaxHealthKey, DataManager.GetInt(DataManager.Enemy1MaxHealthKey) + baseEnemyMaxHPDelta);
        DataManager.SaveIntForce(DataManager.Enemy1DamageKey, DataManager.GetInt(DataManager.Enemy1DamageKey) + baseEnemyDmgDelta);
        DataManager.SaveIntForce(DataManager.Enemy1ExpRewardKey, DataManager.GetInt(DataManager.Enemy1ExpRewardKey) + baseEnemyExpDelta);

        DataManager.SaveIntForce(DataManager.Enemy2MaxHealthKey, DataManager.GetInt(DataManager.Enemy2MaxHealthKey) + heavyEnemyMaxHPDelta);
        DataManager.SaveIntForce(DataManager.Enemy2DamageKey, DataManager.GetInt(DataManager.Enemy2DamageKey) + heavyEnemyDmgDelta);
        DataManager.SaveIntForce(DataManager.Enemy2ExpRewardKey, DataManager.GetInt(DataManager.Enemy2ExpRewardKey) + heavyEnemyExpDelta);

        DataManager.SaveIntForce(DataManager.Monster1MaxHealthKey, DataManager.GetInt(DataManager.Monster1MaxHealthKey) + monster1HPDelta);
        DataManager.SaveIntForce(DataManager.Monster1DamageKey, DataManager.GetInt(DataManager.Monster1DamageKey) + monster1DmgDelta);
        DataManager.SaveIntForce(DataManager.Monster1ExpRewardKey, DataManager.GetInt(DataManager.Monster1ExpRewardKey) + monster1ExpDelta);

        DataManager.SaveIntForce(DataManager.Monster2MaxHealthKey, DataManager.GetInt(DataManager.Monster2MaxHealthKey) + monster2HPDelta);
        DataManager.SaveIntForce(DataManager.Monster2DamageKey, DataManager.GetInt(DataManager.Monster2DamageKey) + monster2DmgDelta);
        DataManager.SaveIntForce(DataManager.Monster2ExpRewardKey, DataManager.GetInt(DataManager.Monster2ExpRewardKey) + monster2ExpDelta);

        eM.RefreshAllEnemiesStatusFromData();
        OnEnemiesLevelUp?.Invoke();
    }

    // ---------------- 奖励关触发 ----------------
    private void TryTriggerRewardLevel()
    {
        float chance = Mathf.Clamp(currentRewardChance, 0f, rewardLevelMaxChance);

        if (Random.value <= chance)
            StartRewardLevel();
    }

    private void StartRewardLevel()
    {
        isRewardLevel = true;

        DataManager.SaveIntForce(DataManager.FirstKillMonster1Key, 0);
        DataManager.SaveIntForce(DataManager.FirstKillMonster2Key, 0);

        eM.RefreshAllEnemiesStatusFromData();
        OnRewardLevelStarted?.Invoke();
    }

    private void EndRewardLevel()
    {
        isRewardLevel = false;

        // 完成后重置为基础概率
        SetRewardChance(rewardLevelBaseChance);

        eM.RefreshAllEnemiesStatusFromData();
        OnRewardLevelEnded?.Invoke();
    }

    public bool IsRewardLevel() => isRewardLevel;
    public float GetRewardStatMultiplier() => rewardLevelStatMultiplier;
    public float GetRewardExpMultiplier() => rewardLevelExpMultiplier;

    private void OnDestroy()
    {
        if (eM != null)
            eM.OnAllEnemiesCleared -= OnWaveCleared;
    }
}
