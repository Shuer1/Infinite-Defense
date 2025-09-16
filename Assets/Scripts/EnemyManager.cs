using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("对象池设置")]
    public EnemyBase basicEnemyPrefab;  // 基础怪物预制体
    public EnemyBase heavyEnemyPrefab;  // 重型怪物预制体
    public int initialPoolSize = 10;    // 初始对象池大小

    [Header("分离设置")]
    public float separationRadius = 1.5f;
    public float separationForce = 3f;

    // 对象池：key=怪物类型，value=待复用的怪物队列
    private Dictionary<EnemyType, Queue<EnemyBase>> enemyPools = new Dictionary<EnemyType, Queue<EnemyBase>>();
    // 当前活跃的怪物列表
    private List<EnemyBase> activeEnemies = new List<EnemyBase>();

    // 所有怪物被清空时触发（用于波数更新）
    public event System.Action OnAllEnemiesCleared;

    // 关键修改：将初始化从Start移到Awake（Awake在Start之前执行）
    void Awake()
    {
        // 初始化对象池
        InitializePool(EnemyType.Basic, basicEnemyPrefab);
        InitializePool(EnemyType.Heavy, heavyEnemyPrefab);
    }

    void Update()
    {
        // 处理活跃怪物的分离逻辑
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            // 跳过非活跃状态的敌人
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

    // 初始化指定类型的对象池
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
            EnemyBase enemy = Instantiate(prefab, transform); // 设置父物体，整理层级
            enemy.gameObject.SetActive(false);
            enemy.enemyType = type; // 确保敌人类型正确
            RegisterEnemyDeathCallback(enemy); // 注册死亡回调
            pool.Enqueue(enemy);
        }
        enemyPools[type] = pool;
    }

    // 从对象池获取怪物，并重置Tag为"Enemy"
    public EnemyBase GetEnemy(EnemyType type, Vector3 spawnPos, Quaternion rotation)
    {
        if (!enemyPools.ContainsKey(type))
        {
            Debug.LogError($"未找到{type}类型的对象池！");
            return null;
        }

        Queue<EnemyBase> pool = enemyPools[type];
        EnemyBase enemy;

        // 池为空时动态扩容
        if (pool.Count == 0)
        {
            enemy = Instantiate(type == EnemyType.Basic ? basicEnemyPrefab : heavyEnemyPrefab, transform);
            enemy.enemyType = type;
            RegisterEnemyDeathCallback(enemy);
        }
        else
        {
            enemy = pool.Dequeue();
        }

        //直接调用重置法
        enemy.ResetEnemyState(spawnPos, rotation);

        /*
        // 激活并设置位置
        enemy.transform.position = spawnPos;
        enemy.transform.rotation = rotation;
        enemy.gameObject.SetActive(true);
        // 重置Tag为敌人（解决重生时Tag仍为DiedEnemy的问题）
        enemy.gameObject.tag = "Enemy";
        // 重置其余信息（待补充）
        enemy.currentHealth = enemy.maxHealth;
        enemy.isDead = false;
        enemy.moveSpeed = enemy.originalMoveSpeed;
        enemy.StopAllCoroutines();

        //重置动画
        */

        RegisterEnemy(enemy); // 注册到活跃列表
        return enemy;
    }

    // 注册敌人到活跃列表
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
            // 确保死亡回调已注册
            RegisterEnemyDeathCallback(enemy);
        }
    }

    // 从活跃列表中注销敌人（用于敌人死亡时调用）
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

    // 处理怪物死亡（回收至对象池）
    private void HandleEnemyDeath(EnemyBase enemy)
    {
        if (enemy == null) return;

        activeEnemies.Remove(enemy);
        // 先禁用对象，等待动画播放完成后再放入对象池
        StartCoroutine(DisableAfterAnimation(enemy));
    }

    // 使用非泛型IEnumerator
    private IEnumerator DisableAfterAnimation(EnemyBase enemy)
    {
        // 假设死亡动画时长约0.5秒，可根据实际动画调整
        yield return new WaitForSeconds(0.5f);
        
        // 再次检查敌人是否为空（防止中途被销毁的极端情况）
        if (enemy != null && enemy.gameObject != null)
        {
            enemy.gameObject.SetActive(false);
            
            // 回收至对应类型的对象池
            if (enemyPools.TryGetValue(enemy.enemyType, out var pool))
            {
                pool.Enqueue(enemy);
            }
            else
            {
                Debug.LogError($"未找到{enemy.enemyType}类型的对象池，无法回收敌人！");
                Destroy(enemy.gameObject); // 万不得已才销毁
            }

            // 检查当前波是否所有怪物都被消灭
            if (activeEnemies.Count == 0)
            {
                OnAllEnemiesCleared?.Invoke();
            }
        }
    }

    // 注册死亡回调（避免重复注册）
    private void RegisterEnemyDeathCallback(EnemyBase enemy)
    {
        if (enemy == null) return;
        // 先移除再添加，防止重复注册
        enemy.OnDeath -= HandleEnemyDeath;
        enemy.OnDeath += HandleEnemyDeath;
    }
}