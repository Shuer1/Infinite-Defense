using UnityEngine;

/// <summary>
/// 子弹发射器，演示如何从对象池获取并发射子弹
/// </summary>
public class BulletSpawner : MonoBehaviour
{
    [Header("子弹预制体")]
    public BulletBasic basicBulletPrefab;
    public BulletExplosive explosiveBulletPrefab;
    public BulletFrozen frozenBulletPrefab;

    [Header("发射参数")]
    public Transform firePoint;
    public float fireRate = 0.5f;
    private float fireTimer;

    private void Start()
    {
        // 初始化对象池
        InitializePools();
    }

    private void Update()
    {
        fireTimer += Time.deltaTime;

        // 示例：按不同键发射不同类型子弹
        if (Input.GetMouseButtonDown(0) && fireTimer >= fireRate)
        {
            FireBullet<BulletBasic>();
            fireTimer = 0;
        }
        else if (Input.GetKeyDown(KeyCode.E) && fireTimer >= fireRate)
        {
            FireBullet<BulletExplosive>();
            fireTimer = 0;
        }
        else if (Input.GetKeyDown(KeyCode.F) && fireTimer >= fireRate)
        {
            FireBullet<BulletFrozen>();
            fireTimer = 0;
        }
    }

    /// <summary>
    /// 初始化对象池
    /// </summary>
    private void InitializePools()
    {
        if (basicBulletPrefab != null)
            BulletPoolManager.Instance.RegisterPool(basicBulletPrefab, 20);
        
        if (explosiveBulletPrefab != null)
            BulletPoolManager.Instance.RegisterPool(explosiveBulletPrefab, 10);

        if (frozenBulletPrefab != null)
            BulletPoolManager.Instance.RegisterPool(frozenBulletPrefab, 10);
    }

    /// <summary>
    /// 发射子弹
    /// </summary>
    /// <typeparam name="T">子弹类型</typeparam>
    private void FireBullet<T>() where T : BulletBase
    {
        T bullet = BulletPoolManager.Instance.GetObject<T>();
        if (bullet != null)
        {
            bullet.transform.position = firePoint.position;
            bullet.transform.rotation = firePoint.rotation;
            bullet.Initialize(firePoint.forward);
        }
    }
}
    