using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 爆炸子弹对象池（单例）
/// </summary>
public class ExplosiveBulletPool : MonoBehaviour
{
    public static ExplosiveBulletPool Instance;

    public GameObject explosiveBulletPrefab; // 爆炸子弹预制体
    public int poolSize = 20;

    private Queue<GameObject> pool = new();

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
    /// 初始化爆炸子弹池
    /// </summary>
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(explosiveBulletPrefab, transform);
            bullet.SetActive(false);
            pool.Enqueue(bullet);
        }
    }

    /// <summary>
    /// 获取爆炸子弹
    /// </summary>
    public GameObject GetBullet(Vector3 position, Quaternion rotation)
    {
        GameObject bullet = pool.Dequeue();

        if (bullet.activeInHierarchy)
        {
            bullet = Instantiate(explosiveBulletPrefab, transform);
        }

        bullet.transform.position = position;
        bullet.transform.rotation = rotation;
        bullet.SetActive(true);

        pool.Enqueue(bullet);
        return bullet;
    }

    /// <summary>
    /// 获取所有爆炸子弹（用于升级同步）
    /// </summary>
    public IEnumerable<GameObject> GetAllBullets()
    {
        return pool.ToArray();
    }

    /// <summary>
    /// 手动回收爆炸子弹
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