using System;
using Microsoft.Unity.VisualStudio.Editor;
using TMPro;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public EnemyManager enemyManager;  // 引用敌人管理器
    public Transform spawn_LineFiled;

    [Header("X轴范围设置")]
    public float xMin = -5f;
    public float xMax = 5f;

    [Header("波数设置")]
    public int baseEnemyCount = 5;     // 第1波基础数量
    public int enemiesPerWaveIncrease = 2; // 每波增加的数量
    public float heavyEnemyBaseChance = 0.1f; // 第1波重型怪物概率
    public float heavyExtraChancePerWave = 0.05f;  // 每波增加的重型概率

    [Header("进度UI信息配置")]
    private int currentWave = 1;
    public UnityEngine.UI.Image uiPartImg;

    void Start()
    {
        //初始化存档波数
        currentWave = DataManager.GetInt(DataManager.CurrentWaveKey);
        // 注册波数完成事件
        enemyManager.OnAllEnemiesCleared += StartNextWave;
        // 启动当前波
        StartWave(currentWave);

    }

    // 启动指定波数
    private void StartWave(int wave)
    {
        Debug.Log($"开始第{wave}波！");
        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager.Instance is null!");
            return;
        }

        UIManager.Instance.AutoShowCurrentWaveUI(uiPartImg, wave);

        int enemyCount = CalculateEnemyCount(wave);
        float heavyChance = CalculateHeavyChance(wave);

        // 生成该波所有怪物
        for (int i = 0; i < enemyCount; i++)
        {
            // 在线型区域上生成
            Vector3 spawnPos = new Vector3(
                UnityEngine.Random.Range(xMin, xMax),
                spawn_LineFiled.position.y,
                spawn_LineFiled.position.z
            );
            
            // 随机决定怪物类型
            EnemyType type = UnityEngine.Random.value < heavyChance ? EnemyType.Heavy : EnemyType.Basic;

            // 从对象池获取怪物
            enemyManager.GetEnemy(type, spawnPos, Quaternion.identity);
        }
    }

    // 计算当前波怪物总数
    private int CalculateEnemyCount(int wave)
    {
        return baseEnemyCount + (wave - 1) * enemiesPerWaveIncrease;
    }

    // 计算当前波重型怪物概率（上限100%）
    private float CalculateHeavyChance(int wave)
    {
        return Mathf.Clamp01(heavyEnemyBaseChance + (wave - 1) * heavyExtraChancePerWave);
    }

    // 开始下一波
    private void StartNextWave()
    {
        currentWave++;
        DataManager.SaveInt(DataManager.CurrentWaveKey, currentWave);
        StartWave(currentWave);
    }
}