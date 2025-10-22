using System;
using System.Collections;
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
    public float heavyExtraChancePerWave = 0.02f;  // 每波增加的重型概率

    [Header("进度UI信息配置")]
    private int currentWave = 1;
    public UnityEngine.UI.Image uiPartImg;
    [Header("波数间隔设置")]
    public float waveStartDelay = 4f; //留出时间作为加载延迟造成的time waste...
    private Coroutine waveStartCoroutine;

    void Start()
    {
        //初始化存档波数
        currentWave = DataManager.GetInt(DataManager.CurrentWaveKey);
        // 注册波数完成事件
        enemyManager.OnAllEnemiesCleared += StartNextWave;
        // 启动当前波
        StartWave(currentWave);

    }

    private void StartWave(int wave) //新增
    {
        Debug.Log($"开始第{wave}波！");
        if (waveStartCoroutine != null)
            StopCoroutine(waveStartCoroutine);
        waveStartCoroutine = StartCoroutine(StartWaveCoroutine(wave));

    }
    
    private IEnumerator StartWaveCoroutine(int wave) //⚠️新增:倒计时3s
    {
        UIManager.Instance.AutoShowCurrentWaveUI(uiPartImg, wave);
        Debug.Log("Next Wave !倒计时开始！");

        yield return new WaitForSeconds(waveStartDelay - 3f);

        for (int count = 3; count >= 1; count--)
        {
            Debug.Log($"SpawnManager 倒计时: {count}");
            UIManager.Instance.UpdateCountdownUI(count);
            yield return new WaitForSeconds(1f);
        }
        UIManager.Instance.HideAllCountdownImages();

        int enemyCount = CalculateEnemyCount(wave);
        float heavyChance = CalculateHeavyChance(wave);

        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 spawnPos = new Vector3(
                UnityEngine.Random.Range(xMin, xMax),
                spawn_LineFiled.position.y,
                spawn_LineFiled.position.z
            );
            
            EnemyType type = UnityEngine.Random.value < heavyChance ? EnemyType.Heavy : EnemyType.Basic;
            enemyManager.GetEnemy(type, spawnPos, Quaternion.identity);
            
            // 敌人生成间隔，避免扎堆
            yield return new WaitForSeconds(0.2f);
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