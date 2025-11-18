using System;
using System.Collections;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public EnemyManager enemyManager;
    public Transform spawn_LineFiled;

    [Header("X轴范围设置")]
    public float xMin = -5f;
    public float xMax = 5f;

    [Header("波数设置")]
    public int maxActiveEnemies = 30;
    public int baseEnemyCount = 3;
    public int enemiesPerWaveIncrease = 2;
    public float heavyEnemyBaseChance = 0.1f;
    public float heavyExtraChancePerWave = 0.02f;
    public event Action OnWaveCompleted;

    [Header("UI与间隔")]
    public UnityEngine.UI.Image uiPartImg;
    public float waveStartDelay = 4f;
    private Coroutine waveStartCoroutine;

    private int currentWave;
    private const string StartWaveKey = "StartWave";

    void Start()
    {
        currentWave = DataManager.GetInt(DataManager.CurrentWaveKey);
        if (enemyManager != null)
            enemyManager.OnAllEnemiesCleared += StartNextWave;
        StartWave(currentWave);
    }

    private void StartWave(int wave)
    {
        Debug.Log($"开始第{wave}波！");
        if (waveStartCoroutine != null)
            StopCoroutine(waveStartCoroutine);
        waveStartCoroutine = StartCoroutine(StartWaveCoroutine(wave));
    }

    private IEnumerator StartWaveCoroutine(int wave)
    {
        UIManager.Instance.AutoShowCurrentWaveUI(uiPartImg, wave);
        yield return new WaitForSeconds(waveStartDelay - 3f);
        for (int count = 3; count >= 1; count--)
        {
            UIManager.Instance.UpdateCountdownUI(count);
            yield return new WaitForSeconds(1f);
        }
        UIManager.Instance.HideAllCountdownImages();

        SoundManager.Instance.PlayEventSFX(StartWaveKey); // 播放开始波数音效

        int enemyCount = CalculateEnemyCount(wave);

        /* ✅ 提前决定整波特殊怪数量（只触发一次） */
        int specialMonsterCount = (wave > 10) ? Mathf.FloorToInt(wave / 10f) : 0;

        int spawned = 0;
        int specialSpawned = 0;

        while (spawned < enemyCount)
        {
            while(enemyManager.activeEnemies.Count >= maxActiveEnemies) // 在生成之前等待同屏数量小于上限
                yield return null;

            /* ✅ 优先生成特殊怪 */
            if (specialSpawned < specialMonsterCount)
            {
                Vector3 spawnPos = new Vector3(
                    UnityEngine.Random.Range(xMin, xMax),
                    spawn_LineFiled.position.y,
                    spawn_LineFiled.position.z);

                EnemyType mType = UnityEngine.Random.value < 0.5f ? EnemyType.Monster1 : EnemyType.Monster2;
                enemyManager.GetEnemy(mType, spawnPos, Quaternion.identity);
                spawned++;
                specialSpawned++;
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            /* ✅ 剩余生成普通怪 */
            float heavyChance = CalculateHeavyChance(wave);
            EnemyType chosenType = UnityEngine.Random.value < heavyChance ? EnemyType.Heavy : EnemyType.Basic;

            Vector3 pos = new Vector3(
                UnityEngine.Random.Range(xMin, xMax),
                spawn_LineFiled.position.y,
                spawn_LineFiled.position.z);

            enemyManager.GetEnemy(chosenType, pos, Quaternion.identity);

            spawned++;
            yield return new WaitForSeconds(0.2f);
        }
    }

    private int CalculateEnemyCount(int wave) => baseEnemyCount + (wave - 1) * enemiesPerWaveIncrease; // 无限制

    private float CalculateHeavyChance(int wave) =>
        Mathf.Clamp01(heavyEnemyBaseChance + (wave - 1) * heavyExtraChancePerWave);

    private void StartNextWave()
    {
        currentWave++;
        DataManager.SaveInt(DataManager.CurrentWaveKey, currentWave);
        StartWave(currentWave);
        OnWaveCompleted?.Invoke();
    }

    private void OnDestroy()
    {
        if (enemyManager != null)
            enemyManager.OnAllEnemiesCleared -= StartNextWave;
    }
}