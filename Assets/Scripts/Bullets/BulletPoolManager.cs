using System.Collections.Generic;
using UnityEngine;

public class BulletPoolManager : MonoBehaviour
{
    public static BulletPoolManager Instance;

    [System.Serializable]
    public class BulletPoolConfig
    {
        public BulletType bulletType;
        public GameObject bulletPrefab;
        public int poolSize = 10;
        public bool allowExpand = true;
    }

    [Header("子弹池配置列表")]
    public List<BulletPoolConfig> poolConfigs = new List<BulletPoolConfig>();

    // ✅ 使用 BulletType 作为 Key，而非 string
    private readonly Dictionary<BulletType, Queue<GameObject>> bulletPools = new Dictionary<BulletType, Queue<GameObject>>();

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
    /// ✅ 从对象池获取子弹实例（使用 BulletType 而非 string）
    /// </summary>
    public GameObject GetBullet(BulletType bulletType, Vector3 position, Quaternion rotation)
    {
        if (!bulletPools.ContainsKey(bulletType))
        {
            Debug.LogWarning($"[BulletPoolManager] 未找到子弹类型：{bulletType}");
            return null;
        }

        var pool = bulletPools[bulletType];
        var config = poolConfigs.Find(c => c.bulletType == bulletType);

        GameObject bullet = (pool.Count > 0) ? pool.Dequeue() : null;

        if (bullet == null || bullet.activeInHierarchy)
        {
            if (config != null && config.allowExpand)
            {
                bullet = Instantiate(config.bulletPrefab, transform);
            }
            else
            {
                Debug.LogWarning($"[BulletPoolManager] {bulletType}池已耗尽，且未开启扩容功能！");
                return null;
            }
        }

        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.SetActive(true);

        pool.Enqueue(bullet);
        return bullet;
    }

    /// <summary>
    /// ✅ 回收子弹
    /// </summary>
    public void ReturnBullet(BulletType bulletType, GameObject bullet)
    {
        if (bullet == null) return;
        bullet.SetActive(false);

        if (bulletPools.ContainsKey(bulletType))
        {
            if (!bulletPools[bulletType].Contains(bullet))
            {
                bulletPools[bulletType].Enqueue(bullet);
            }
        }
    }

    public IEnumerable<GameObject> GetAllBullets(BulletType bulletType)
    {
        if (bulletPools.ContainsKey(bulletType))
            return bulletPools[bulletType].ToArray();
        return new List<GameObject>();
    }
}
