using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 冰冻子弹对象池（单例）
/// </summary>
public class FrostBulletPool : MonoBehaviour
{
    public static FrostBulletPool Instance;

    public GameObject frostBulletPrefab; // 冰冻子弹预制体
    public int poolSize = 20;

    private Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
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
    /// 初始化冰冻子弹池
    /// </summary>
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(frostBulletPrefab, transform);
            bullet.SetActive(false);
            pool.Enqueue(bullet);
        }
    }

    /// <summary>
    /// 获取冰冻子弹
    /// </summary>
    public GameObject GetBullet(Vector3 position, Quaternion rotation)
    {
        GameObject bullet = pool.Dequeue();

        if (bullet.activeInHierarchy)
        {
            bullet = Instantiate(frostBulletPrefab, transform);
        }

        bullet.transform.position = position;
        bullet.transform.rotation = rotation;
        bullet.SetActive(true);

        pool.Enqueue(bullet);
        return bullet;
    }

    /// <summary>
    /// 获取所有冰冻子弹（用于升级同步）
    /// </summary>
    public IEnumerable<GameObject> GetAllBullets()
    {
        return pool.ToArray();
    }

    /// <summary>
    /// 手动回收冰冻子弹
    /// </summary>
    public void ReturnBullet(GameObject bullet)
    {
        if (bullet != null && !pool.Contains(bullet))
        {
            bullet.SetActive(false);
            pool.Enqueue(bullet);
        }
    }
}