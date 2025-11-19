using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EnemyManager : MonoBehaviour
{
    // 保留原公开变量名（序列化字段不丢失，外部引用无感知）
    [Header("对象池设置")]
    public EnemyBase basicEnemyPrefab;
    public EnemyBase heavyEnemyPrefab;
    public EnemyBase Monster1Prefab;
    public EnemyBase Monster2Prefab;
    public int initialPoolSize = 5;

    [Header("分离设置")]
    public float separationRadius = 1.5f;
    public float separationForce = 3f;

    // 保留原私有容器
    private Dictionary<EnemyType, Queue<EnemyBase>> enemyPools = new();
    public List<EnemyBase> activeEnemies = new();

    // 保留原事件
    public event System.Action OnAllEnemiesCleared;

    // 保留原Awake逻辑
    void Awake()
    {
        InitializePool(EnemyType.Basic, basicEnemyPrefab, 5);
        InitializePool(EnemyType.Heavy, heavyEnemyPrefab, 5);
        InitializePool(EnemyType.Monster1, Monster1Prefab, 3);
        InitializePool(EnemyType.Monster2, Monster2Prefab, 3);
    }

    // 保留原Update逻辑
    void Update()
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (!activeEnemies[i].gameObject.activeInHierarchy) continue;

            Vector3 separation = Vector3.zero;
            for (int j = 0; j < activeEnemies.Count; j++)
            {
                if (i == j) continue;
                if (!activeEnemies[j].gameObject.activeInHierarchy) continue;

                Vector3 distance = activeEnemies[i].transform.position - activeEnemies[j].transform.position;
                float distMagnitude = distance.magnitude;

                if (distMagnitude > 0 && distMagnitude < separationRadius)
                {
                    separation += distance.normalized * (separationForce / distMagnitude);
                }
            }
            activeEnemies[i].transform.position += separation * Time.deltaTime;
        }
    }

    // 保留原重载方法（避免内部调用报错）
    private void InitializePool(EnemyType type, EnemyBase prefab)
    {
        if (prefab == null)
        {
            Debug.LogError($"初始化{type}对象池失败：预制体为空！");
            return;
        }

        Queue<EnemyBase> pool = new Queue<EnemyBase>();
        for (int i = 0; i < initialPoolSize; i++)
        {
            EnemyBase enemy = Instantiate(prefab, transform);
            enemy.gameObject.SetActive(false);
            enemy.enemyType = type;
            RegisterEnemyDeathCallback(enemy);
            pool.Enqueue(enemy);
        }
        enemyPools[type] = pool;
    }

    // 保留原自定义数量重载
    private void InitializePool(EnemyType type, EnemyBase prefab, int customCount)
    {
        if (prefab == null)
        {
            Debug.LogError($"初始化{type}对象池失败：预制体为空！");
            return;
        }

        Queue<EnemyBase> pool = new Queue<EnemyBase>();
        for (int i = 0; i < customCount; i++)
        {
            EnemyBase enemy = Instantiate(prefab, transform);
            enemy.gameObject.SetActive(false);
            enemy.enemyType = type;
            RegisterEnemyDeathCallback(enemy);
            pool.Enqueue(enemy);
        }
        enemyPools[type] = pool;
    }

    // 保留原GetEnemy方法（外部调用无感知）
    public EnemyBase GetEnemy(EnemyType type, Vector3 spawnPos, Quaternion rotation)
    {
        if (!enemyPools.ContainsKey(type))
        {
            Debug.LogError($"未找到{type}类型的对象池！");
            return null;
        }

        Queue<EnemyBase> pool = enemyPools[type];
        EnemyBase enemy;

        if (pool.Count == 0)
        {
            EnemyBase prefabToInstantiate = type switch
            {
                EnemyType.Basic => basicEnemyPrefab,
                EnemyType.Heavy => heavyEnemyPrefab,
                EnemyType.Monster1 => Monster1Prefab,
                EnemyType.Monster2 => Monster2Prefab,
                _ => null
            };

            if (prefabToInstantiate == null)
            {
                Debug.LogError($"初始化{type}对象池失败：预制体为空！");
                return null;
            }

            enemy = Instantiate(prefabToInstantiate, spawnPos, rotation, transform);

            enemy.enemyType = type;
            RegisterEnemyDeathCallback(enemy);
            enemy.ResetEnemyState(spawnPos, rotation);
        }
        else
        {
            enemy = pool.Dequeue();
            enemy.ResetEnemyState(spawnPos, rotation);
        }

        RegisterEnemy(enemy);
        return enemy;
    }

    // 保留原注册/注销方法
    public void RegisterEnemy(EnemyBase enemy)
    {
        if (enemy == null)
        {
            Debug.LogError("尝试注册空敌人对象！");
            return;
        }

        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
            RegisterEnemyDeathCallback(enemy);
        }
    }

    public void UnregisterEnemy(EnemyBase enemy)
    {
        if (enemy == null)
        {
            Debug.LogError("尝试注销空敌人对象！");
            return;
        }

        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }

    // 保留原死亡处理逻辑
    private void HandleEnemyDeath(EnemyBase enemy)
    {
        if (enemy == null) return;
        StartCoroutine(DisableAfterAnimation(enemy));
    }

    // 保留原协程方法
    private IEnumerator DisableAfterAnimation(EnemyBase enemy)
    {
        yield return new WaitForSeconds(0.5f);

        if (enemy != null && enemy.gameObject != null)
        {
            enemy.ResetEnemyState(enemy.transform.position, enemy.transform.rotation);
            enemy.gameObject.SetActive(false);

            if (enemyPools.TryGetValue(enemy.enemyType, out var pool))
            {
                pool.Enqueue(enemy);
            }
            else
            {
                Debug.LogError($"未找到{enemy.enemyType}类型的对象池，无法回收敌人！");
                Destroy(enemy.gameObject);
            }

            if (activeEnemies.Count == 0)
            {
                OnAllEnemiesCleared?.Invoke();
            }
        }
    }

    // 保留原刷新方法（外部调用无感知）
    public void RefreshAllEnemiesStatusFromData()
    {
        Debug.Log("<color=cyan>统一刷新敌人属性...</color>");

        int enemiesLevel = DataManager.GetInt(DataManager.EnemiesLevelKey);

        foreach (var kvp in enemyPools)
        {
            foreach (var enemy in kvp.Value)
                ApplyUpdatedStats(enemy, enemiesLevel);
        }

        foreach (var enemy in activeEnemies)
            ApplyUpdatedStats(enemy, enemiesLevel);
    }

    // 修复奖励倍数应用（核心功能，不修改方法签名）
    private void ApplyUpdatedStats(EnemyBase enemy, int level)
    {
        if (enemy == null) return;

        enemy.EnemiesLevel = level;
        
        bool isReward = GlobalDifficultyCurveController.Instance?.IsRewardLevel() ?? false;
        float statMultiplier = isReward ? GlobalDifficultyCurveController.Instance.GetRewardStatMultiplier() : 1f;
        float expMultiplier = isReward ? GlobalDifficultyCurveController.Instance.GetRewardExpMultiplier() : 1f;

        // 保留原switch逻辑，仅添加倍数应用
        switch (enemy.enemyType)
        {
            case EnemyType.Basic:
                enemy.maxHealth = Mathf.RoundToInt(DataManager.GetInt(DataManager.Enemy1MaxHealthKey) * statMultiplier);
                enemy.damage = Mathf.RoundToInt(DataManager.GetInt(DataManager.Enemy1DamageKey) * statMultiplier);
                enemy.expReward = Mathf.RoundToInt(DataManager.GetInt(DataManager.Enemy1ExpRewardKey) * expMultiplier);
                break;
            case EnemyType.Heavy:
                enemy.maxHealth = Mathf.RoundToInt(DataManager.GetInt(DataManager.Enemy2MaxHealthKey) * statMultiplier);
                enemy.damage = Mathf.RoundToInt(DataManager.GetInt(DataManager.Enemy2DamageKey) * statMultiplier);
                enemy.expReward = Mathf.RoundToInt(DataManager.GetInt(DataManager.Enemy2ExpRewardKey) * expMultiplier);
                break;
            case EnemyType.Monster1:
                enemy.maxHealth = Mathf.RoundToInt(DataManager.GetInt(DataManager.Monster1MaxHealthKey) * statMultiplier);
                enemy.damage = Mathf.RoundToInt(DataManager.GetInt(DataManager.Monster1DamageKey) * statMultiplier);
                enemy.expReward = Mathf.RoundToInt(DataManager.GetInt(DataManager.Monster1ExpRewardKey) * expMultiplier);
                break;
            case EnemyType.Monster2:
                enemy.maxHealth = Mathf.RoundToInt(DataManager.GetInt(DataManager.Monster2MaxHealthKey) * statMultiplier);
                enemy.damage = Mathf.RoundToInt(DataManager.GetInt(DataManager.Monster2DamageKey) * statMultiplier);
                enemy.expReward = Mathf.RoundToInt(DataManager.GetInt(DataManager.Monster2ExpRewardKey) * expMultiplier);
                break;
        }

        enemy.currentHealth = Mathf.Min(enemy.currentHealth, enemy.maxHealth);
    }

    // 保留原死亡回调注册方法
    private void RegisterEnemyDeathCallback(EnemyBase enemy)
    {
        if (enemy == null) return;
        enemy.OnDeath -= HandleEnemyDeath;
        enemy.OnDeath += HandleEnemyDeath;
    }
}