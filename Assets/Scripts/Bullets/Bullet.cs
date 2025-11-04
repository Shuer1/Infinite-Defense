using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("基础属性")]
    public float speed = 15f;
    public int damage = 10;
    public float lifeTime = 2f;

    protected BulletType bulletType = BulletType.Normal;
    private float currentLifeTime = 0f;

    // 子弹每次从对象池激活时，重新读取最新的 DataManager 数据
    protected virtual void OnEnable()
    {
        currentLifeTime = 0f;
        damage = DataManager.GetInt(DataManager.BaseBulletDamageKey, damage);
    }

    protected virtual void Update()
    {
        // 基础移动
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // 生命周期计时 & 自动回收
        currentLifeTime += Time.deltaTime;
        if (currentLifeTime >= lifeTime)
        {
            ReturnToPool();
        }
    }

    // 基础伤害逻辑（普通子弹）
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null && !enemy.isDead)
        {
            enemy.TakeDamage(damage);
        }

        ReturnToPool(); 
    }

    // 放回对象池
    protected virtual void ReturnToPool()
    {
        if (BulletPoolManager.Instance != null)
        {
            BulletPoolManager.Instance.ReturnBullet(bulletType, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
