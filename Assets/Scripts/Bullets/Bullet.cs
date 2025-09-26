using JetBrains.Annotations;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 50;
    public float lifeTime = 2f; //控制子弹回收
    private float timer;

    void OnEnable()
    {
        timer = 0f;
    }
    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            gameObject.SetActive(false); // 自动回收
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
            gameObject.SetActive(false);
        }
    }
}
