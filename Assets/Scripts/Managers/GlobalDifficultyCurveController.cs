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
    [SerializeField] private int baseEnemyMaxHPDeltaValue = 50;
    [SerializeField] private int heavyEnemyMaxHPDeltaValue = 100;
    [SerializeField] private int baseEnemyDamageDeltaValue = 5;
    [SerializeField] private int heavyEnemyDamageDeltaValue = 10;
    [SerializeField] private int baseEnemyExpDeltaValue = 10;
    [SerializeField] private int heavyEnemyExpDeltaValue = 20;
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
        clearEnemyWaveCount = 0;

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
    }

    void SyncData()
    {
        Debug.Log("初始化数据");
    }

    void UpdateData()
    {
        DataManager.SaveInt(DataManager.Enemy1MaxHealthKey, DataManager.GetInt(DataManager.Enemy1MaxHealthKey) + baseEnemyMaxHPDeltaValue);
        DataManager.SaveInt(DataManager.Enemy1ExpRewardKey, DataManager.GetInt(DataManager.Enemy1ExpRewardKey) + baseEnemyExpDeltaValue);
        DataManager.SaveInt(DataManager.Enemy1DamageKey, DataManager.GetInt(DataManager.Enemy1DamageKey) + baseEnemyDamageDeltaValue);

        DataManager.SaveInt(DataManager.Enemy2MaxHealthKey, DataManager.GetInt(DataManager.Enemy2MaxHealthKey) + heavyEnemyMaxHPDeltaValue);
        DataManager.SaveInt(DataManager.Enemy2ExpRewardKey, DataManager.GetInt(DataManager.Enemy2ExpRewardKey) + heavyEnemyExpDeltaValue);
        DataManager.SaveInt(DataManager.Enemy2DamageKey, DataManager.GetInt(DataManager.Enemy2DamageKey) + heavyEnemyDamageDeltaValue);
    }
    
    void OnDestroy()
    {
        if (eM != null)
            eM.OnAllEnemiesCleared -= UpgradeEnemys;
    }
}
