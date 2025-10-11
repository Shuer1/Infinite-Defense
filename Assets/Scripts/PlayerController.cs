using System.Threading.Tasks;
using UnityEditor;

//using Unity.PlasticSCM.Editor.WebApi;
//using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Attributes")]
    public int health = 100; //Data1
    public int currentHealth = 100;
    public int level = 1; //Data2
    public int experience = 0;
    public int experienceToNextLevel = 100; //Data3

    [Header("Movement & Shooting")]
    public Transform firePoint;
    public float fireRate = 0.3f; //Data4
    private float fireTimer;
    public float moveSpeed = 5f;
    public bool isDead = false;
    private bool isMoving = false;
    private float moveThreshold = 0.01f;

    [Header("虚拟移动轮盘")]
    public VirtualJoystick joystick;

    [Header("自动锁敌设置")]
    public float lockOnRange = 10f;
    public float targetCheckInterval = 0.5f; //检测频率
    private float targetCheckTimer = 0f;
    private EnemyBase currentTarget;
    private EnemyManager enemyManager;

    private Rigidbody rb;
    private Animator animator;
    [Header("玩家音效")]
    public AudioSource moveSound;
    public AudioSource shootSound;
    [Header("按钮Btn_UI_Element")]
    public Button btn_ResetLive;
    [Header("子弹概率配置")]
    [Range(0, 100)]
    public int normalBulletChance = 100; //Data5
    [Range(0, 100)]
    public int explosiveBulletChance = 0; //Data6

    void Awake()
    {
        enemyManager = FindObjectOfType<EnemyManager>();
    }
    void Start()
    {
        SyncPlayerData(); //同步初始化玩家数据
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isDead) return;
        // 移动输入
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        #if UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
                if (joystick != null)
                {
                    h = joystick.Horizontal;
                    v = joystick.Vertical;
                }
        #endif

        Vector3 move = new Vector3(h, 0, v) * moveSpeed;
        rb.velocity = move;

        // 判断是否在移动（输入值超过阈值）
        bool isMovingNow = (h * h + v * v) > moveThreshold * moveThreshold;
        // 用平方和判断，避免开方运算，效率更高

        // 状态变化时更新音效
        if (isMovingNow != isMoving)
        {
            isMoving = isMovingNow;

            if (isMoving)
            {
                // 开始移动：播放音效
                if (moveSound != null && !moveSound.isPlaying)
                {
                    moveSound.Play();
                }
            }
            else
            {
                // 停止移动：停止音效
                if (moveSound != null && moveSound.isPlaying)
                {
                    moveSound.Stop();
                }
            }
        }

        // 射击（计时器更新，自动攻击）
        fireTimer += Time.deltaTime;
        /*取消手动鼠标按下攻击
        if (Input.GetMouseButton(0) && fireTimer >= fireRate)
        {
            Shoot(firePoint.rotation);
            fireTimer = 0f;
        }
        */
        AutoLockAndShoot();
        // 始终朝向目标
        if (currentTarget != null)
        {
            Vector3 targetDirection = currentTarget.transform.position - transform.position;
            targetDirection.y = 0; // 保持Y轴不变，避免上下旋转
            if (targetDirection.sqrMagnitude > 0.01f) // 确保有距离才旋转
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
        }

        AnimatorFunc();
    }

    void SyncPlayerData()  //初始化玩家数据
    {
        //玩家自身数据
        health = DataManager.GetInt(DataManager.PlayerMaxHealthKey);
        level = DataManager.GetInt(DataManager.PlayerLevelKey);
        experienceToNextLevel = DataManager.GetInt(DataManager.NextLevelExpKey);
        fireRate = DataManager.GetFloat(DataManager.PlayerShootSpeedKey);
        //不同类型子弹射击几率
        normalBulletChance = DataManager.GetInt(DataManager.NormalBulletChanceKey);
        explosiveBulletChance = DataManager.GetInt(DataManager.ExplosiveBulletChanceKey);

        Debug.Log("初始化玩家信息完成!");
    }

    void Shoot(Quaternion bulletRotation)
    {
        ClampProbabilities(); // 每次射击前强制修正概率，确保合法性

        int randomValue = Random.Range(0, 100);
        int cumulativeProbability = 0;

        // 普通子弹
        cumulativeProbability += normalBulletChance;
        if (randomValue < cumulativeProbability)
        {
            if (BulletPool.Instance == null)
            {
                Debug.LogError("普通子弹池未初始化!请检查BulletPool的Instance设置");
                return;
            }
            BulletPool.Instance.GetBullet(firePoint.position, bulletRotation);
            shootSound.Play();
            return;
        }

        // 爆炸子弹
        cumulativeProbability += explosiveBulletChance;
        if (randomValue < cumulativeProbability)
        {
            if (ExplosiveBulletPool.Instance == null)
            {
                Debug.LogError("爆炸子弹池未初始化!请检查ExplosiveBulletPool的Instance设置");
                return;
            }
            ExplosiveBulletPool.Instance.GetBullet(firePoint.position, bulletRotation);
            shootSound.Play();
            return;
        }

        // 冰冻子弹
        if (FrostBulletPool.Instance == null)
        {
            Debug.LogError("冰冻子弹池未初始化!请检查FrostBulletPool的Instance设置");
            return;
        }
        FrostBulletPool.Instance.GetBullet(firePoint.position, bulletRotation);
        shootSound.Play();
    }

    public void TakeDamage(int damage)
    {
        Debug.Log($"玩家受到伤害: {damage}");
        animator.SetTrigger("Hit");
        currentHealth = Mathf.Max(currentHealth - damage, 0);
        UIManager.Instance.UpdateAndShowPlayerHP(currentHealth, health);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void GainExp(int exp)
    {
        Debug.Log($"获得经验: {exp}");
        experience += exp;
        if (experience >= experienceToNextLevel)
        {
            LevelUp();
        }

        UIManager.Instance.ShowAndUpdatePlayerExp(experience, experienceToNextLevel); //统一更新经验信息
    }

    void LevelUp()
    {
        level++;
        experience = 0;
        experienceToNextLevel += 50; // 每次升级需要更多经验：应优化为使用非线性增长，前期较容易，后期越来越难
        //升级刷新恢复血量
        currentHealth = health;
        UIManager.Instance.UpdateAndShowPlayerHP(currentHealth, health);
        UpgradeManager.Instance.ShowUpgradeOptions();
        //保存数据
        DataManager.SaveInt(DataManager.PlayerLevelKey, level);
        DataManager.SaveInt(DataManager.NextLevelExpKey,experienceToNextLevel);
    }

    void Die()
    {

        isDead = true;
        animator.SetTrigger("Die");
        Debug.Log("Player Died");
        GameManager.Instance.GameOver();
    }

    void AnimatorFunc()
    {
        animator.SetBool("Run", rb.velocity.magnitude > 0);
        animator.SetBool("Shoot", currentTarget != null && fireTimer >= fireRate);
    }

    public void ResetLive() //❌待实现：复活功能 - 用于激励广告！提高游戏宽容度!
    {
        Debug.Log("选择复活!");
        currentHealth = health;
        isDead = false;
        animator.SetTrigger("ResetLive");
    }
    private void ClampProbabilities()
    {
        // 限制普通子弹概率范围
        normalBulletChance = Mathf.Clamp(normalBulletChance, 0, 100);
        // 限制爆炸子弹概率范围（剩余可用概率内）
        explosiveBulletChance = Mathf.Clamp(explosiveBulletChance, 0, 100 - normalBulletChance);
        // 此时冰冻子弹概率自动为非负且总和为100
    }

    /// 调整子弹概率（用于升级系统）
    public void AdjustBulletChances(int normalDelta, int explosiveDelta)
    {
        normalBulletChance += normalDelta;
        explosiveBulletChance += explosiveDelta;
        ClampProbabilities(); // 确保修正
    }

    public void SetBulletChance(BulletType bulletType, int newPercentage)
    {
        switch (bulletType)
        {
            case BulletType.Normal:
                normalBulletChance = newPercentage;
                break;
            case BulletType.Explosive:
                explosiveBulletChance = newPercentage;
                break;
            case BulletType.Frost:
                int delta = 100 - normalBulletChance - explosiveBulletChance - newPercentage;
                explosiveBulletChance += delta;
                break;
        }
        ClampProbabilities(); // 确保修正
    }

    private void AutoLockAndShoot()
    {
        targetCheckTimer += Time.deltaTime;
        if (targetCheckTimer >= targetCheckInterval)
        {
            UpdateTarget();
            targetCheckTimer = 0f;
        }

        if (currentTarget != null && fireTimer >= fireRate)
        {
            Vector3 targetDirection = currentTarget.transform.position - firePoint.position;
            targetDirection.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            Shoot(targetRotation);
            fireTimer = 0f;
        }
    }

    private void UpdateTarget()
    {
        if (IsTargetValid(currentTarget))
        {
            return;
        }

        currentTarget = FindBestTarget();
    }

    private bool IsTargetValid(EnemyBase target)
    {
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;

        // 使用平方距离判断，避免开方运算
        float sqrDistance = (target.transform.position - transform.position).sqrMagnitude;
        return sqrDistance <= lockOnRange * lockOnRange;
    }
    
    // 寻找最佳目标（范围内最近的敌人）
    private EnemyBase FindBestTarget()
    {
        if (enemyManager == null || enemyManager.activeEnemies.Count == 0)
        {
            return null;
        }

        float closestSqrDistance = lockOnRange * lockOnRange;
        EnemyBase closestEnemy = null;
        Vector3 playerPos = transform.position;

        // 遍历活跃敌人（EnemyManager已维护有效列表）
        foreach (var enemy in enemyManager.activeEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            // 计算平方距离（性能更优）
            float sqrDistance = (enemy.transform.position - playerPos).sqrMagnitude;

            // 只记录范围内更近的敌人
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }
}

public enum BulletType
{
    Normal,    // 普通子弹
    Explosive, // 爆炸子弹
    Frost      // 冰冻子弹
}