using Unity.VisualScripting;
using UnityEngine;
using System.Collections;

public abstract class EnemyBase : MonoBehaviour
{
    // 动画参数常量（避免魔法字符串）
    private const string AnimIdle = "Idle";
    private const string AnimRun = "Run";
    private const string AnimAttack = "Attack";
    private const string AnimDie = "Die";

    public EnemyType enemyType;
    public event System.Action<EnemyBase> OnDeath; // 死亡事件
    public int maxHealth;
    public int currentHealth;
    public int damage;
    public float moveSpeed;
    public float originalMoveSpeed; // 用于减速效果的原始速度保存
    public int expReward;
    public int scoreReward;

    public bool isDead = false;
    private Animator animator;
    protected Transform player;
    private EnemyManager enemyManager;
    private PlayerController pc;

    // 可在Inspector中编辑的范围参数
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float chaseRange = 2.5f; // 比攻击范围稍大作为缓冲
    [SerializeField] private float attackCooldown = 1f; // 攻击冷却时间
    private float lastAttackTime = 0f;
    private string currentState = AnimIdle; // 初始状态设为Idle

    void Start()
    {
        gameObject.tag = "Enemy";
        currentHealth = maxHealth;
        // 新增：校验移动速度是否合理，避免初始为0
        if (moveSpeed <= 0)
        {
            Debug.Log($"{gameObject.name}的moveSpeed设置为0或负数,自动设为默认值2f", this);
            moveSpeed = 2f;
        }
        originalMoveSpeed = moveSpeed; // 确保原始速度正确保存

        // 安全获取玩家引用
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
        {
            pc = player.GetComponent<PlayerController>();
            if (pc == null)
            {
                Debug.LogWarning("Player上未找到PlayerController组件", this);
            }
        }
        else
        {
            Debug.LogWarning("未找到标签为Player的物体", this);
        }

        // 初始化动画组件
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Enemy未添加Animator组件", this);
        }

        // 注册到EnemyManager
        GameObject pool = GameObject.Find("EnemyManager");
        if (pool != null)
        {
            enemyManager = pool.GetComponent<EnemyManager>();
            if (enemyManager != null)
            {
                enemyManager.RegisterEnemy(this);
            }
            else
            {
                Debug.LogWarning("EnemyManager上未找到EnemyManager组件", this);
            }
        }
        else
        {
            Debug.LogWarning("未找到EnemyManager物体", this);
        }
    }

    void Update()
    {
        // 死亡状态不执行任何逻辑
        if (isDead) return;

        // 玩家死亡或不存在时停止行动
        if (pc?.isDead ?? true || player == null)
        {
            ChangeAniStatus(currentState, AnimIdle);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        // 距离大于追逐范围：移动追逐
        if (distance > chaseRange)
        {
            ChangeAniStatus(AnimAttack, AnimRun);
            MoveTowardsPlayer();
        }
        // 距离在攻击范围内且冷却结束：攻击
        else if (distance <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            ChangeAniStatus(AnimRun, AnimAttack);
            Attack(damage);
            lastAttackTime = Time.time;
        }
        // 在缓冲区域内且当前不是攻击状态：继续移动到攻击位置
        else if (currentState != AnimAttack)
        {
            MoveTowardsPlayer();
        }
    }

    // 移动到玩家的通用方法
    private void MoveTowardsPlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
        transform.LookAt(player);
    }

    // 动画状态切换（参数名更清晰）
    void ChangeAniStatus(string fromState, string toState)
    {
        if (currentState == toState || animator == null) return;

        animator.SetBool(toState, true);
        if (fromState != AnimIdle) // Idle状态不需要手动关闭（作为默认状态）
        {
            animator.SetBool(fromState, false);
        }
        currentState = toState;
    }

    public void TakeDamage(int dmg)
    {
        if (isDead) return; // 已死亡不接受伤害

        currentHealth -= dmg;
        Debug.Log($"敌人受到伤害: {dmg}, 剩余生命值: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        if (isDead) return; // 防止重复调用

        isDead = true;
        gameObject.tag = "DiedEnemy";
        Debug.Log("Enemy Died");
        ChangeAniStatus(currentState, AnimDie);

        // 触发死亡事件
        OnDeath?.Invoke(this);

        // 从EnemyManager注销
        if (enemyManager != null)
        {
            enemyManager.UnregisterEnemy(this);
        }

        // 安全给予奖励
        if (player != null && pc != null)
        {
            pc.GainExp(expReward);
        }
        GameManager.Instance?.AddScore(scoreReward);

        // 移除销毁代码，改为由对象池处理回收
        // 原错误代码：Destroy(gameObject, 0.5f);
    }

    void Attack(int damage)
    {
        ChangeAniStatus(AnimRun, AnimAttack);
        // 攻击逻辑通过动画事件调用AttackPlayer()
    }

    public void AttackPlayer()
    {
        if (pc == null || isDead) return;

        pc.TakeDamage(damage);
    }

    // 实现减速效果
    public void ApplySlow(float percentage, float duration)
    {
        if (isDead) return;

        StartCoroutine(SlowCoroutine(percentage, duration));
    }

    private IEnumerator SlowCoroutine(float slowPercentage, float duration)
    {
        float slowFactor = 1 - (slowPercentage / 100f);
        moveSpeed *= slowFactor;

        yield return new WaitForSeconds(duration);

        // 恢复原始速度（防止多次减速叠加问题）
        moveSpeed = originalMoveSpeed;
    }
    
    /// <summary>
    /// 重置敌人状态（用于从对象池取出时）
    /// </summary>
    /// <param name="spawnPos">重生位置</param>
    /// <param name="rotation">重生旋转</param>
    public void ResetEnemyState(Vector3 spawnPos, Quaternion rotation)
    {
        // 重置位置和旋转
        transform.position = spawnPos;
        transform.rotation = rotation;

        // 激活对象
        gameObject.SetActive(true);

        // 重置标签
        gameObject.tag = "Enemy";

        // 重置生命状态
        currentHealth = maxHealth;
        isDead = false;

        // 强化速度重置，确保不为0
        //moveSpeed = originalMoveSpeed <= 0 ? 2f : originalMoveSpeed;
        moveSpeed = originalMoveSpeed;
        StopAllCoroutines(); // 终止可能的减速协程

        // 重置攻击冷却
        lastAttackTime = 0f;

        // 重置动画状态
        if (animator != null)
        {
            animator.SetBool(AnimDie, false);
            animator.SetBool(AnimIdle, true);
        }
        currentState = AnimIdle;

        // 关键修复：重新获取玩家引用（避免旧引用失效）
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player != null)
        {
            pc = player.GetComponent<PlayerController>();
            if (pc == null)
            {
                Debug.LogWarning("Player上未找到PlayerController组件", this);
            }
        }
        else
        {
            Debug.LogWarning("未找到标签为Player的物体", this);
        }

        // 可选：重置其他临时状态（如Buff/Debuff、AI目标等）
        //ResetCustomStates(); // 留给子类实现自定义重置逻辑
    }

    /// <summary>
    /// 子类可重写此方法，添加自定义状态重置
    /// </summary>
    protected virtual void ResetCustomStates()
    {
        // 例如：清除特殊攻击标记、重置技能CD等
    }
}