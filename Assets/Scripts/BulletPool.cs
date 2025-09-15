using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;

    public GameObject bulletPrefab;
    public int poolSize = 20;

    private Queue<GameObject> pool = new Queue<GameObject>();

    void Awake()
    {
        // 完善单例模式，防止重复实例
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
    /// 初始化对象池
    /// </summary>
    private void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab, transform);
            bullet.SetActive(false);
            pool.Enqueue(bullet);
        }
    }

    public GameObject GetBullet(Vector3 position, Quaternion rotation)
    {
        GameObject bullet = pool.Dequeue();

        // 如果子弹正在活跃状态（可能被重复使用），创建新的子弹
        if (bullet.activeInHierarchy)
        {
            bullet = Instantiate(bulletPrefab, transform);
        }

        // 设置位置和旋转
        bullet.transform.position = position;
        bullet.transform.rotation = rotation;
        bullet.SetActive(true);

        // 放回队列（继续循环使用）
        pool.Enqueue(bullet);
        return bullet;
    }

    /// <summary>
    /// 提供获取对象池中所有子弹的方法（用于升级时同步更新）
    /// </summary>
    /// <returns>所有子弹的GameObject列表</returns>
    public IEnumerable<GameObject> GetAllBullets()
    {
        return pool.ToArray();  // 将队列转换为数组返回，避免直接操作内部队列
    }

    /// <summary>
    /// 回收子弹（可选：如果需要手动回收可以调用）
    /// </summary>
    public void ReturnBullet(GameObject bullet)
    {
        if (bullet != null)
        {
            bullet.SetActive(false);
            if (!pool.Contains(bullet))  // 避免重复入队
            {
                pool.Enqueue(bullet);
            }
        }
    }
}
    