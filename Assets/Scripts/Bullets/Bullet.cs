using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("基础属性")]
    public float speed = 15f;        // 默认飞行速度
    public int damage;               // 子弹基础伤害
    public float lifeTime = 2f;      // 存活时长（超过则回收）
    private float timer;
    protected BulletType bulletType = BulletType.Normal;

    void OnEnable()
    {
        timer = 0f;
    }

    void Start()
    {
        // 初始化子弹伤害（可升级时修改 DamageKey 即可）
        damage = DataManager.GetInt(DataManager.BaseBulletDamageKey);
    }

    void Update()
    {
        // 子弹前进
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // 计时自动回收
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            ReturnToPool();
        }
    }

    // 可重写，子弹击中敌人时执行
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            enemy?.TakeDamage(damage);

            ReturnToPool();
        }
    }

    // ✅ 改为传枚举，不用 string 判断
    protected void ReturnToPool()
    {
        gameObject.SetActive(false);

        if (BulletPoolManager.Instance != null)
        {
            BulletPoolManager.Instance.ReturnBullet(bulletType, gameObject);
        }
    }
}
