using JetBrains.Annotations;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f; //默认不变/不升级等
    public int damage; //Data1
    public float lifeTime = 2f; //控制子弹回收
    private float timer;

    protected string bulletType = BulletType.Normal.ToString(); //默认

    void OnEnable()
    {
        timer = 0f;
    }

    void Start()
    {
        damage = DataManager.GetInt(DataManager.BaseBulletDamageKey);  //初始化
    }
    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            ReturnToPool();
        }
    }

    // 添加可被重写的触发方法
    protected virtual void OnTriggerEnter(Collider other)
    {
        // 基类默认实现（可以留空或实现基础逻辑）
        if (other.CompareTag("Enemy"))
        {
            // 基础子弹逻辑（如果需要）
            EnemyBase enemy = other.GetComponent<EnemyBase>();
            enemy?.TakeDamage(damage);

            // 回收子弹
            ReturnToPool();
        }
    }

    protected void ReturnToPool()
    {
        gameObject.SetActive(false);
        if (BulletPoolManager.Instance != null && !string.IsNullOrEmpty(bulletType)) 
        {
            BulletPoolManager.Instance.ReturnBullet(bulletType, gameObject);
        }
    }
}
