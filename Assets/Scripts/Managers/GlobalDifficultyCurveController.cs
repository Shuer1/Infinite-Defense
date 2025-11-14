using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalDifficultyCurveController : MonoBehaviour
{
    public static GlobalDifficultyCurveController Instance;
    public EnemyManager eM;
    public int clearEnemyWaveCount = 0;
    public event System.Action OnEnemiesLevelUp;
    // 敌人1升级变化值
    private int baseEnemyMaxHPDeltaValue = 50;
    private int baseEnemyDamageDeltaValue = 10;
    private int baseEnemyExpDeltaValue = 5;
    // 敌人2升级变化值
    private int heavyEnemyMaxHPDeltaValue = 100;
    private int heavyEnemyDamageDeltaValue = 15;
    private int heavyEnemyExpDeltaValue = 15;
    // 怪物1升级变化值
    private int Monster1MaxHPDeltaValue = 350;
    private int Monster1DamageDeltaValue = 25;
    private int Monster1ExpDeltaValue = 65;
    // 怪物2升级变化值
    private int Monster2MaxHPDeltaValue = 500;
    private int Monster2DamageDeltaValue = 35;
    private int Monster2ExpDeltaValue = 80;
    // 每次升级的波数阈值
    private const int upgradeThreshold = 10;
    public string EnemiesUPKey = "EnemiesUP";
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
            SoundManager.Instance.PlayEventSFX(EnemiesUPKey);
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
