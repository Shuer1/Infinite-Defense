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

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        animator = GetComponent<Animator>();
        animator.SetBool("Run", true);
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (isDead) return;

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
        currentHealth -= dmg;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        isDead = true;
        animator.SetBool("Run", false);
        animator.SetBool("Die", true);
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
                Die(); // 敌人攻击后死亡（可选规则）
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
}
