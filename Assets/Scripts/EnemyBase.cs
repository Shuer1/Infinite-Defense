using Unity.VisualScripting;
using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    public int maxHealth;
    [SerializeField] private int currentHealth;
    public int damage;
    public float moveSpeed;
    public int expReward;
    public int scoreReward;
    
    private bool isDead = false;
    private Animator animator;
    protected Transform player;
    private EnemyManager enemyManager;
    private PlayerController pc;

    //增加状态变量和攻击间隔控制（以下变量用于动画控制）
    private string currentState = "Idle";
    private float attackCooldown = 1f; // 攻击冷却时间
    private float lastAttackTime = 0f;
    private float attackRange = 2f;
    private float chaseRange = 2.5f; // 比攻击范围稍大，作为缓冲

    void Start()
    {
        gameObject.tag = "Enemy";
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (player == null) {
            return;
        }
        pc = player.GetComponent<PlayerController>();

        animator = GetComponent<Animator>();
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
        if (pc.isDead)
        {
            animator.SetBool("Run", false);
            animator.SetBool("Attack", false);
            return;
        }

        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            
            // 距离大于追逐范围，保持奔跑状态
            if (distance > chaseRange)
            {
                ChangeAniStatus("Attack", "Run");
                Vector3 dir = (player.position - transform.position).normalized;
                transform.position += dir * moveSpeed * Time.deltaTime;
                transform.LookAt(player);
            }
            // 距离在攻击范围内，且攻击冷却已结束
            else if (distance <= attackRange && Time.time - lastAttackTime >= attackCooldown)
            {
                ChangeAniStatus("Run", "Attack");
                Attack(damage);
                lastAttackTime = Time.time; // 记录攻击时间
            }
            // 在缓冲区域内，保持当前状态
            else if (currentState == "Attack")
            {
                // 已经在攻击状态，保持不动
            }
            else
            {
                // 已经在奔跑状态，继续移动到攻击位置
                Vector3 dir = (player.position - transform.position).normalized;
                transform.position += dir * moveSpeed * Time.deltaTime;
                transform.LookAt(player);
            }
        }
    }

    void ChangeAniStatus(string currentStatus, string targetStatus)
    {
        // 只有当前状态与目标状态不同时才切换
        if (currentState != targetStatus)
        {
            animator.SetBool(targetStatus, true);
            animator.SetBool(currentStatus, false);
            currentState = targetStatus; // 更新状态变量
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
        gameObject.tag = "DiedEnemy";
        Debug.Log("Enemy Died");
        isDead = true;
        ChangeAniStatus("Run","Die");

        // 自动注销
        if (enemyManager != null)
        {
            enemyManager.UnregisterEnemy(this);
        }

        player.GetComponent<PlayerController>().GainExp(expReward);
        GameManager.Instance.AddScore(scoreReward);
        Destroy(gameObject, 0.5f); //等待0.5s播放死亡动画
    }
    void Attack(int damage)
    {
        ChangeAniStatus("Run", "Attack");
        //AttackPlayer(); //该方法放在攻击动画中执行：Frame28执行事件
    }

    public void AttackPlayer()
    {
        if (player != null)
        {
            //PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(damage);
            }
        }
    }

    public void ApplySlow(float percentage, float duration)
    {
        //StartCoroutine(SlowCoroutine(percentage, duration));
    }



}
