using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// 敌人基础类（最终正式版）
/// ✅ 只攻击防御塔，不攻击玩家
/// ✅ 保留玩家对敌人造成伤害、经验与得分系统
/// ✅ 防御塔被摧毁后游戏结束，所有敌人停止活动
/// ✅ 动画、减速、对象池、死亡事件等逻辑完整
/// </summary>
public abstract class EnemyBase : MonoBehaviour
{
    // 动画参数常量
    private const string AnimIdle = "Idle";
    private const string AnimRun = "Run";
    private const string AnimAttack = "Attack";
    private const string AnimDie = "Die";

    [Header("Enemy Attributes")]
    public EnemyType enemyType;
    public int maxHealth;
    public int currentHealth;
    public int damage;
    public float moveSpeed = 2f;
    public float originalMoveSpeed;
    public int expReward;      // 玩家获得经验值
    public int scoreReward;    // 玩家得分奖励

    [Header("Runtime Flags")]
    public bool isDead = false;

    private Animator animator;
    private EnemyManager enemyManager;
    private DefenseTowerController defenseTower;
    private PlayerController pc;

    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float chaseRange = 2.5f;
    [SerializeField] private float attackCooldown = 1f;
    private float lastAttackTime = 0f;
    private string currentState = AnimIdle;
    private float originalAnimSpeed = 1.0f;
    private bool isFrozen = false;

    // =============================
    // 生命周期
    // =============================
    protected void Start()
    {
        gameObject.tag = "Enemy";

        if (moveSpeed <= 0)
        {
            Debug.LogWarning($"{gameObject.name} 的 moveSpeed <= 0，自动设为默认值 2f", this);
            moveSpeed = 2f;
        }
        originalMoveSpeed = moveSpeed;

        // ✅ 获取防御塔
        defenseTower = DefenseTowerController.Instance;
        if (defenseTower == null)
            Debug.LogWarning("未找到 DefenseTowerController.Instance", this);

        // ✅ 获取玩家（仅用于经验奖励，不参与敌人逻辑）
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            pc = playerObj.GetComponent<PlayerController>();
            if (pc == null)
                Debug.LogWarning("Player 上未找到 PlayerController 组件", this);
        }

        // ✅ 动画组件
        animator = GetComponent<Animator>();
        if (animator != null)
            originalAnimSpeed = animator.speed;
        else
            Debug.LogWarning("Enemy 未添加 Animator 组件", this);

        // ✅ 注册到 EnemyManager
        GameObject pool = GameObject.Find("EnemyManager");
        if (pool != null)
        {
            enemyManager = pool.GetComponent<EnemyManager>();
            if (enemyManager != null)
                enemyManager.RegisterEnemy(this);
            else
                Debug.LogWarning("EnemyManager 上未找到 EnemyManager 组件", this);
        }
        else
        {
            Debug.LogWarning("未找到 EnemyManager 物体", this);
        }
    }

    // =============================
    // 每帧逻辑
    // =============================
    void Update()
    {
        if (isDead) return;

        // ✅ 塔不存在或死亡时 → 停止行动并触发游戏结束
        if (defenseTower == null || defenseTower.currentHealth <= 0 || defenseTower.isBroken)
        {
            ChangeAniStatus(currentState, AnimIdle);
            return;
        }

        // ✅ 追踪与攻击逻辑（仅塔）
        Transform target = defenseTower.transform;
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > chaseRange)
        {
            ChangeAniStatus(AnimAttack, AnimRun);
            MoveTowardsTarget(target);
        }
        else if (distance <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            ChangeAniStatus(AnimRun, AnimAttack);
            AttackTower();
            lastAttackTime = Time.time;
        }
        else if (currentState != AnimAttack)
        {
            MoveTowardsTarget(target);
        }
    }

    // =============================
    // 移动逻辑
    // =============================
    private void MoveTowardsTarget(Transform target)
    {
        if (target == null) return;

        Vector3 dir = target.position - transform.position;
        dir.y = 0;
        dir.Normalize();

        Vector3 newPos = transform.position + dir * moveSpeed * Time.deltaTime;
        newPos.y = 0;
        transform.position = newPos;

        //看向目标后锁定X和Z
        transform.LookAt(target);
        Vector3 currentRot = transform.localEulerAngles;
        transform.localEulerAngles = new Vector3(0, currentRot.y, 0);
    }

    // =============================
    // 动画控制
    // =============================
    private void ChangeAniStatus(string fromState, string toState)
    {
        if (currentState == toState || animator == null) return;

        animator.SetBool(toState, true);
        if (fromState != AnimIdle)
            animator.SetBool(fromState, false);

        currentState = toState;
    }

    // =============================
    // 受伤与死亡（玩家攻击触发）
    // =============================
    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        // 显示伤害数字（通过管理器）
        DamageTextManager.Instance?.ShowDamageText(dmg, transform.position);
        
        currentHealth -= dmg;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;
        gameObject.tag = "DiedEnemy";
        ChangeAniStatus(currentState, AnimDie);

        // 通知管理器与回调
        OnDeath?.Invoke(this);
        enemyManager?.UnregisterEnemy(this);

        // ✅ 给玩家奖励（经验 + 分数）
        if (pc != null)
        {
            pc.GainExp(expReward);
        }
        GameManager.Instance?.AddScore(scoreReward);
    }

    // =============================
    // 攻击防御塔
    // =============================
    void AttackTower()
    {
        ChangeAniStatus(AnimRun, AnimAttack);
        // 动画事件调用 AttackTowerHit()
    }

    public void AttackTowerHit()
    {
        if (defenseTower != null && defenseTower.currentHealth > 0)
        {
            defenseTower.TakeDamage(damage);
        }
    }

    // =============================
    // 减速控制
    // =============================
    public void ApplySlow(float percentage, float duration)
    {
        if (isDead || isFrozen) return;
        StartCoroutine(SlowCoroutine(percentage, duration));
    }

    private IEnumerator SlowCoroutine(float slowPercentage, float duration)
    {
        isFrozen = true;
        float slowFactor = 1 - (slowPercentage / 100f);
        moveSpeed *= slowFactor;

        if (animator != null)
            animator.speed = slowFactor;

        yield return new WaitForSeconds(duration);

        isFrozen = false;
        moveSpeed = originalMoveSpeed;
        if (animator != null)
            animator.speed = originalAnimSpeed;
    }

    // =============================
    // 对象池复用重置
    // =============================
    public void ResetEnemyState(Vector3 spawnPos, Quaternion rotation)
    {
        // 重置位置和旋转
        transform.position = new Vector3(spawnPos.x, 0, spawnPos.z); //强制锁定Y轴位置为0
        transform.rotation = rotation;
        
        // 重新激活对象
        gameObject.SetActive(true);
        gameObject.tag = "Enemy";

        // 重置基本状态
        currentHealth = maxHealth;
        isDead = false;
        moveSpeed = originalMoveSpeed;
        
        // 停止所有协程并重置攻击计时器
        StopAllCoroutines();
        lastAttackTime = 0f;
        
        // 重置冻结状态
        isFrozen = false;

        // 重置动画状态机
        if (animator != null)
        {
            // 清除所有动画状态
            animator.SetBool(AnimDie, false);
            animator.SetBool(AnimRun, false);
            animator.SetBool(AnimAttack, false);
            animator.SetBool(AnimIdle, true);
            animator.speed = originalAnimSpeed; // 重置动画播放速度
        }
        currentState = AnimIdle;

        // 重新获取防御塔引用（重要：确保引用的是当前有效的防御塔实例）
        defenseTower = DefenseTowerController.Instance;
        if (defenseTower == null)
            Debug.LogWarning("未找到 DefenseTowerController.Instance", this);
            
        // 重新获取玩家引用
        if (pc == null) 
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                pc = playerObj.GetComponent<PlayerController>();
            }
        }
    }

    // =============================
    // 事件
    // =============================
    public event System.Action<EnemyBase> OnDeath;
}