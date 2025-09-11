using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对象池管理器，负责管理所有可复用对象
/// </summary>
public class BulletPoolManager : MonoBehaviour
{
    // 单例实例
    public static BulletPoolManager Instance { get; private set; }

    // 存储所有对象池
    private Dictionary<Type, ObjectPool> pools = new Dictionary<Type, ObjectPool>();

    private void Awake()
    {
        // 确保单例唯一性
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// 注册对象池
    /// </summary>
    /// <typeparam name="T">子弹类型</typeparam>
    /// <param name="prefab">预制体</param>
    /// <param name="initialSize">初始大小</param>
    public void RegisterPool<T>(T prefab, int initialSize = 10) where T : BulletBase
    {
        Type type = typeof(T);
        if (!pools.ContainsKey(type))
        {
            pools[type] = new ObjectPool(prefab, transform, initialSize);
        }
    }

    /// <summary>
    /// 从对象池获取对象
    /// </summary>
    /// <typeparam name="T">子弹类型</typeparam>
    /// <returns>获取的对象</returns>
    public T GetObject<T>() where T : BulletBase
    {
        Type type = typeof(T);
        if (pools.TryGetValue(type, out ObjectPool pool))
        {
            return pool.GetObject() as T;
        }
        
        Debug.LogError($"对象池未注册类型: {type.Name}");
        return null;
    }

    /// <summary>
    /// 回收对象到池中
    /// </summary>
    /// <param name="bullet">要回收的子弹</param>
    public void ReturnObject(BulletBase bullet)
    {
        if (bullet == null) return;
        
        Type type = bullet.GetType();
        if (pools.TryGetValue(type, out ObjectPool pool))
        {
            pool.ReturnObject(bullet);
        }
        else
        {
            Debug.LogError($"没有找到{type.Name}的对象池，直接销毁");
            Destroy(bullet.gameObject);
        }
    }

    /// <summary>
    /// 内部对象池类
    /// </summary>
    public class ObjectPool
    {
        private BulletBase prefab;
        private Transform parent;
        private Queue<BulletBase> inactiveObjects = new Queue<BulletBase>();

        public ObjectPool(BulletBase prefab, Transform parent, int initialSize)
        {
            this.prefab = prefab;
            this.parent = parent;
            
            // 预先创建初始数量的对象
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        /// <summary>
        /// 创建新对象
        /// </summary>
        /// <returns>新创建的对象</returns>
        private BulletBase CreateNewObject()
        {
            BulletBase obj = Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            obj.Pool = this;
            inactiveObjects.Enqueue(obj);
            return obj;
        }

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        /// <returns>可用对象</returns>
        public BulletBase GetObject()
        {
            if (inactiveObjects.Count == 0)
            {
                // 池为空时，创建新对象
                CreateNewObject();
            }

            BulletBase obj = inactiveObjects.Dequeue();
            obj.gameObject.SetActive(true);
            obj.ResetBullet(); // 重置子弹状态
            return obj;
        }

        /// <summary>
        /// 回收对象到池中
        /// </summary>
        /// <param name="obj">要回收的对象</param>
        public void ReturnObject(BulletBase obj)
        {
            obj.gameObject.SetActive(false);
            inactiveObjects.Enqueue(obj);
        }
    }
}
    