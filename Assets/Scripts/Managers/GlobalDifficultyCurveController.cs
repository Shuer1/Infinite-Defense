using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalDifficultyCurveController : MonoBehaviour
{
    public static GlobalDifficultyCurveController Instance;
    public EnemyManager eM;
    public int clearEnemyWaveCount = 0;
    public event System.Action OnEnemiesLevelUp;
    [Header("升级增量配置")]
    [Header("Enemy1升级提升值")]
    [SerializeField] private int baseEnemyMaxHPDeltaValue = 50;
    [SerializeField] private int heavyEnemyMaxHPDeltaValue = 100;
    [SerializeField] private int baseEnemyDamageDeltaValue = 5;
    [Header("Enemy2升级提升值")]
    [SerializeField] private int heavyEnemyDamageDeltaValue = 10;
    [SerializeField] private int baseEnemyExpDeltaValue = 10;
    [SerializeField] private int heavyEnemyExpDeltaValue = 20;
    [Header("Monster1升级提升值")]
    [SerializeField] private int Monster1MaxHPDeltaValue = 350;
    [SerializeField] private int Monster1DamageDeltaValue = 20;
    [SerializeField] private int Monster1ExpDeltaValue = 65;
    [Header("Monster2升级提升值")]
    [SerializeField] private int Monster2MaxHPDeltaValue = 500;
    [SerializeField] private int Monster2DamageDeltaValue = 25;
    [SerializeField] private int Monster2ExpDeltaValue = 80;
    private const int upgradeThreshold = 10;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    void Start()
    {
        SyncData();

        if(eM == null)
            eM = FindObjectOfType<EnemyManager>();

        if (eM != null)
            eM.OnAllEnemiesCleared += UpgradeEnemys;
        else
            Debug.LogError("EnemyManager未引用");
    }

    public void UpgradeEnemys() //统一升级
    {
        clearEnemyWaveCount++;

        if (clearEnemyWaveCount >= upgradeThreshold)
        {
            clearEnemyWaveCount = 0;
            UpdateData();
            OnEnemiesLevelUp?.Invoke();
            Debug.Log("完成10波敌人的清除,敌人全部升级！");

            if (eM != null)
                eM.RefreshAllEnemiesStatusFromData();
        }

        UIManager.Instance.UpdateWaveToLevelUP(clearEnemyWaveCount);
        DataManager.SaveIntForce(DataManager.ClearEnemiesCountKey, clearEnemyWaveCount); //保存清除的敌人数
    }

    void SyncData()
    {
        clearEnemyWaveCount = DataManager.GetInt(DataManager.ClearEnemiesCountKey, 0);
        UIManager.Instance.UpdateWaveToLevelUP(DataManager.GetInt(DataManager.ClearEnemiesCountKey, 0));
        Debug.Log("初始化数据");
    }

    void UpdateData()
    {
        int currentLevel = DataManager.GetInt(DataManager.EnemiesLevelKey, 1);
        DataManager.SaveIntForce(DataManager.EnemiesLevelKey, currentLevel + 1);

        DataManager.SaveInt(DataManager.Enemy1MaxHealthKey, DataManager.GetInt(DataManager.Enemy1MaxHealthKey) + baseEnemyMaxHPDeltaValue);
        DataManager.SaveInt(DataManager.Enemy1ExpRewardKey, DataManager.GetInt(DataManager.Enemy1ExpRewardKey) + baseEnemyExpDeltaValue);
        DataManager.SaveInt(DataManager.Enemy1DamageKey, DataManager.GetInt(DataManager.Enemy1DamageKey) + baseEnemyDamageDeltaValue);

        DataManager.SaveInt(DataManager.Enemy2MaxHealthKey, DataManager.GetInt(DataManager.Enemy2MaxHealthKey) + heavyEnemyMaxHPDeltaValue);
        DataManager.SaveInt(DataManager.Enemy2ExpRewardKey, DataManager.GetInt(DataManager.Enemy2ExpRewardKey) + heavyEnemyExpDeltaValue);
        DataManager.SaveInt(DataManager.Enemy2DamageKey, DataManager.GetInt(DataManager.Enemy2DamageKey) + heavyEnemyDamageDeltaValue);

        DataManager.SaveInt(DataManager.Monster1MaxHealthKey, DataManager.GetInt(DataManager.Monster1MaxHealthKey) + Monster1MaxHPDeltaValue);
        DataManager.SaveInt(DataManager.Monster1ExpRewardKey, DataManager.GetInt(DataManager.Monster1ExpRewardKey) + Monster1ExpDeltaValue);
        DataManager.SaveInt(DataManager.Monster1DamageKey, DataManager.GetInt(DataManager.Monster1DamageKey) + Monster1DamageDeltaValue);

        DataManager.SaveInt(DataManager.Monster2MaxHealthKey, DataManager.GetInt(DataManager.Monster2MaxHealthKey) + Monster2MaxHPDeltaValue);
        DataManager.SaveInt(DataManager.Monster2ExpRewardKey, DataManager.GetInt(DataManager.Monster2ExpRewardKey) + Monster2ExpDeltaValue);
        DataManager.SaveInt(DataManager.Monster2DamageKey, DataManager.GetInt(DataManager.Monster2DamageKey) + Monster2DamageDeltaValue);
    }
    
    void OnDestroy()
    {
        if (eM != null)
            eM.OnAllEnemiesCleared -= UpgradeEnemys;
    }
}
