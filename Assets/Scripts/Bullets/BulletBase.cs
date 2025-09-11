using UnityEngine;

/// <summary>
/// 子弹基类，所有子弹类型的父类
/// </summary>
public abstract class BulletBase : MonoBehaviour
{
    [Header("基础属性")]
    [Tooltip("子弹伤害值")]
    public int damage = 1;
    
    [Tooltip("子弹飞行速度")]
    public float speed = 10f;
    
    [Tooltip("子弹生命周期(秒)")]
    public float lifeTime = 5f;
    
    [Tooltip("子弹碰撞层")]
    public LayerMask targetLayer;

    // 子弹飞行方向
    protected Vector3 direction;
    
    // 生命周期计时器
    protected float lifeTimer;
    
    // 关联的对象池
    internal BulletPoolManager.ObjectPool Pool { get; set; }

    protected virtual void Update()
    {
        // 移动子弹
        Move();
        
        // 更新生命周期
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            DestroyBullet();
        }
    }

    /// <summary>
    /// 初始化子弹
    /// </summary>
    /// <param name="fireDirection">发射方向</param>
    public virtual void Initialize(Vector3 fireDirection)
    {
        direction = fireDirection.normalized;
        lifeTimer = 0;
        transform.forward = direction;
    }

    /// <summary>
    /// 子弹移动逻辑
    /// </summary>
    protected virtual void Move()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// 碰撞检测
    /// </summary>
    protected virtual void OnTriggerEnter(Collider other)
    {
        // 检查是否命中目标层
        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            OnHitTarget(other);
        }
    }

    /// <summary>
    /// 命中目标处理（子类实现具体逻辑）
    /// </summary>
    /// <param name="target">命中的目标</param>
    protected abstract void OnHitTarget(Collider target);

    /// <summary>
    /// 销毁子弹（实际是回收到对象池）
    /// </summary>
    public virtual void DestroyBullet()
    {
        if (Pool != null)
        {
            // 回收到对象池
            BulletPoolManager.Instance.ReturnObject(this);
        }
        else
        {
            // 如果没有对象池，直接销毁
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 重置子弹状态，供对象池复用
    /// </summary>
    public virtual void ResetBullet()
    {
        lifeTimer = 0;
        // 可以在这里重置其他需要重置的状态
    }

    /// <summary>
    /// 检查是否是有效目标
    /// </summary>
    protected virtual bool IsTarget(Collider collider)
    {
        return ((1 << collider.gameObject.layer) & targetLayer) != 0;
    }
}
    