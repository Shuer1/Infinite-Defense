using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalDifficultyCurveController : MonoBehaviour
{
    public static GlobalDifficultyCurveController Instance;
    public EnemyManager eM;
    public int clearEnemyWaveCount = 0;
    private int baseEnemyMaxHPDeltaValue = 50;
    private int heavyEnemyMaxHPDeltaValue = 100;
    private int baseEnemyDamageDeltaValue = 5;
    private int heavyEnemyDamageDeltaValue = 10;
    private int baseEnemyExpDeltaValue = 10;
    private int heavyEnemyExpDeltaValue = 20;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        SyncData();
        eM.OnAllEnemiesCleared += UpgradeEnemys;

    }

    public void UpgradeEnemys() //统一升级
    {
        clearEnemyWaveCount++;

        if (clearEnemyWaveCount > 10)
        {
            UpdateData();
            Debug.Log("敌人全部升级！");
        }
        else
        {
            Debug.Log($"第{clearEnemyWaveCount}波敌人结束！");
        }

        
    }

    void SyncData()
    {
        Debug.Log("初始化数据");
    }

    void UpdateData()
    {
        DataManager.SaveInt("BaseEnemyMaxHealth", DataManager.GetInt("BaseEnemyMaxHealth") + baseEnemyMaxHPDeltaValue);
        DataManager.SaveInt("BaseEnemyExpEReward", DataManager.GetInt("BaseEnemyExpEReward") + baseEnemyExpDeltaValue);
        DataManager.SaveInt("BaseBulletDamage", DataManager.GetInt("BaseBulletDamage") + baseEnemyDamageDeltaValue);

        DataManager.SaveInt("HeavyEnemyMaxHealth", DataManager.GetInt("HeavyEnemyMaxHealth") + heavyEnemyMaxHPDeltaValue);
        DataManager.SaveInt("HeavyEnemyExpEReward", DataManager.GetInt("HeavyEnemyExpEReward") + heavyEnemyExpDeltaValue);
        DataManager.SaveInt("HeavyEnemyDamage", DataManager.GetInt("HeavyEnemyDamage") + heavyEnemyDamageDeltaValue);
    }
    
    void OnDestroy()
    {
        eM.OnAllEnemiesCleared -= UpgradeEnemys;
    }
}
