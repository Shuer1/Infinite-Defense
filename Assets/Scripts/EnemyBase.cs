using Unity.VisualScripting;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    public int maxHealth;
    public int damage;
    public float moveSpeed;
    public int expReward;
    private int currentHealth;

    private Animator animator;
    private bool isDead = false;
    protected Transform player;

    private EnemyManager enemyManager;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        animator = GetComponent<Animator>();
        animator.SetBool("Run", true);
        currentHealth = maxHealth;

        // 自动注册到 EnemyManager
        GameObject pool = GameObject.Find("EnemyPool");
        if (pool != null)
        {
            enemyManager = pool.GetComponent<EnemyManager>();
            if (enemyManager != null)
            {
                enemyManager.RegisterEnemy(this);
            }
        }
    }

    void Update()
    {
        if (player != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
            transform.LookAt(player);

            if (Vector3.Distance(transform.position, player.position) < 2f)
            {
                Attack(damage);
            }
        }
    }

    public void TakeDamage(int dmg)
    {
        Debug.Log($"敌人受到伤害: {dmg}");
        currentHealth -= dmg;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log("Enemy Died");
        isDead = true;
        animator.SetBool("Run", false);
        animator.SetBool("Die", true);

        // 自动注销
        if (enemyManager != null)
        {
            enemyManager.UnregisterEnemy(this);
        }

        player.GetComponent<PlayerController>().GainExp(expReward);
        Destroy(gameObject, 0.5f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(damage);
            }
        }
    }

    void Attack(int damage)
    {
        animator.SetBool("Attack", true);
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(damage);
            }
        }
    }

    // 结束追踪和攻击
    /*
    void EndTraceAndAttack(bool playerisdead)
    {

        animator.SetBool("Run", !playerisdead);
        animator.SetBool("Attack", !playerisdead);
        return;
    }
    */

}
