using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public EnemyManager enemyManager;  // 引用敌人管理器
    public float spawnRadius = 10f;    // 生成范围

    [Header("波数设置")]
    public int baseEnemyCount = 5;     // 第1波基础数量
    public int enemiesPerWaveIncrease = 2; // 每波增加的数量
    public float heavyEnemyBaseChance = 0.1f; // 第1波重型怪物概率
    public float heavyChancePerWave = 0.05f;  // 每波增加的重型概率

    private int currentWave;

    void Start()
    {
        // ⚠️测试阶段：默认从第1波开始
        currentWave = 1;

        // ✅正式版：从存档加载当前波数
        //currentWave = SaveManager.GetCurrentWave();   core initiation

        // 注册波数完成事件
        enemyManager.OnAllEnemiesCleared += StartNextWave;

        // 启动当前波
        StartWave(currentWave);
    }

    // 启动指定波数
    private void StartWave(int wave)
    {
        Debug.Log($"开始第{wave}波！");
        int enemyCount = CalculateEnemyCount(wave);
        float heavyChance = CalculateHeavyChance(wave);

        // 生成该波所有怪物
        for (int i = 0; i < enemyCount; i++)
        {
            // 随机生成位置（平面上）
            Vector3 spawnPos = Random.onUnitSphere * spawnRadius;
            spawnPos.y = 0;

            // 随机决定怪物类型
            EnemyType type = Random.value < heavyChance ? EnemyType.Heavy : EnemyType.Basic;

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
        return Mathf.Clamp01(heavyEnemyBaseChance + (wave - 1) * heavyChancePerWave);
    }

    // 开始下一波
    private void StartNextWave()
    {
        currentWave++;
        SaveManager.SaveCurrentWave(currentWave); // 保存进度
        StartWave(currentWave);
    }
}