using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🧩 通用多类型子弹对象池管理器（单例）
/// 仅负责子弹的创建、回收，不修改任何其他逻辑。
/// </summary>
public class BulletPoolManager : MonoBehaviour
{
    public static BulletPoolManager Instance;

    [System.Serializable]
    public class BulletPoolConfig
    {
        public string bulletType;              // 子弹类型标识（如 "Frost", "Lightning", "Fire"）
        public GameObject bulletPrefab;        // 子弹预制体
        public int poolSize = 20;              // 初始池大小
        public bool allowExpand = true;        // 是否允许自动扩容
    }

    [Header("子弹池配置列表")]
    public List<BulletPoolConfig> poolConfigs = new List<BulletPoolConfig>();

    // 每种类型的对象池独立管理
    private readonly Dictionary<string, Queue<GameObject>> bulletPools = new Dictionary<string, Queue<GameObject>>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            InitializeAllPools();
        }
    }

    /// <summary>
    /// 初始化所有类型的子弹池
    /// </summary>
    private void InitializeAllPools()
    {
        foreach (var config in poolConfigs)
        {
            if (config.bulletPrefab == null)
            {
                Debug.LogWarning($"[BulletPoolManager] 子弹类型 {config.bulletType} 缺少 Prefab，跳过初始化。");
                continue;
            }

            Queue<GameObject> pool = new Queue<GameObject>();

            for (int i = 0; i < config.poolSize; i++)
            {
                GameObject bullet = Instantiate(config.bulletPrefab, transform);
                bullet.SetActive(false);
                pool.Enqueue(bullet);
            }

            bulletPools[config.bulletType] = pool;
        }
    }

    /// <summary>
    /// 从指定类型的对象池中获取一个子弹实例
    /// </summary>
    public GameObject GetBullet(string bulletType, Vector3 position, Quaternion rotation)
    {
        if (!bulletPools.ContainsKey(bulletType))
        {
            Debug.LogWarning($"[BulletPoolManager] 未找到子弹类型：{bulletType}");
            return null;
        }

        var pool = bulletPools[bulletType];
        var config = poolConfigs.Find(c => c.bulletType == bulletType);

        GameObject bullet = null;
        if (pool.Count > 0)
        {
            bullet = pool.Dequeue();
        }

        // 若池中对象仍在使用或池为空
        if (bullet == null || bullet.activeInHierarchy)
        {
            if (config != null && config.allowExpand)
            {
                bullet = Instantiate(config.bulletPrefab, transform);
            }
            else
            {
                Debug.LogWarning($"[BulletPoolManager] {bulletType} 池耗尽，且未启用自动扩容！");
                return null;
            }
        }

        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.SetActive(true);

        // 重新入队以保持循环使用
        pool.Enqueue(bullet);
        return bullet;
    }

    /// <summary>
    /// 回收子弹到对应对象池
    /// </summary>
    public void ReturnBullet(string bulletType, GameObject bullet)
    {
        if (bullet == null) return;

        bullet.SetActive(false);

        if (bulletPools.ContainsKey(bulletType))
        {
            var pool = bulletPools[bulletType];
            if (!pool.Contains(bullet))
                pool.Enqueue(bullet);
        }
        else
        {
            Debug.LogWarning($"[BulletPoolManager] 无法回收：未注册类型 {bulletType}");
        }
    }

    /// <summary>
    /// 获取当前类型的所有子弹（用于升级同步）
    /// </summary>
    public IEnumerable<GameObject> GetAllBullets(string bulletType)
    {
        if (bulletPools.ContainsKey(bulletType))
            return bulletPools[bulletType].ToArray();
        return new List<GameObject>();
    }
}
