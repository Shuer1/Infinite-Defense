using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ⚡ 闪电子弹对象池（单例）
/// </summary>
public class LightningBulletPool : MonoBehaviour
{
    public static LightningBulletPool Instance;

    [Header("闪电子弹预制体与参数")]
    public GameObject lightningBulletPrefab; // 闪电子弹预制体
    [Tooltip("对象池初始数量")]
    public int poolSize = 20;
    [Tooltip("是否允许池自动扩容")]
    public bool allowExpand = true;

    private Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        // ✅ 单例初始化
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            InitializePool();
        }
    }

    /// <summary>
    /// 初始化闪电子弹池
    /// </summary>
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(lightningBulletPrefab, transform);
            bullet.SetActive(false);
            pool.Enqueue(bullet);
        }
    }

    /// <summary>
    /// 从对象池中获取闪电子弹
    /// </summary>
    /// <param name="position">生成位置</param>
    /// <param name="rotation">生成角度</param>
    /// <returns>闪电子弹实例</returns>
    public GameObject GetBullet(Vector3 position, Quaternion rotation)
    {
        GameObject bullet = null;

        // ✅ 从队列中取出下一个对象
        if (pool.Count > 0)
        {
            bullet = pool.Dequeue();
        }

        // ✅ 如果对象池为空或取出的对象仍在使用中（未回收）
        if (bullet == null || bullet.activeInHierarchy)
        {
            if (allowExpand)
            {
                bullet = Instantiate(lightningBulletPrefab, transform);
            }
            else
            {
                Debug.LogWarning("[LightningBulletPool] 对象池不足，且未启用自动扩容！");
                return null;
            }
        }

        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.SetActive(true);

        // ✅ 重新放回队列尾部（循环利用）
        pool.Enqueue(bullet);
        return bullet;
    }

    /// <summary>
    /// 获取当前池中所有闪电子弹（用于同步参数或升级调整）
    /// </summary>
    public IEnumerable<GameObject> GetAllBullets()
    {
        return pool.ToArray();
    }

    /// <summary>
    /// 手动回收闪电子弹
    /// </summary>
    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null) return;

        bullet.SetActive(false);

        // ✅ 确保不会重复入队（若 bullet 已在队列中）
        if (!pool.Contains(bullet))
        {
            pool.Enqueue(bullet);
        }
    }
}
