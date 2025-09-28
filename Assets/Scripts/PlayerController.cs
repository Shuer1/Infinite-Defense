using System.Threading.Tasks;
//using Unity.PlasticSCM.Editor.WebApi;
//using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Attributes")]
    public int health = 100;
    public int currentHealth = 100;
    //public int playerDamage; 玩家伤害基于子弹伤害，该属性目前: No Usage
    public int level = 1;
    public int experience = 0;
    public int experienceToNextLevel = 100;

    [Header("Movement & Shooting")]
    public Transform firePoint;
    public float fireRate = 0.3f;
    private float fireTimer;
    public float moveSpeed = 5f;
    public bool isDead = false;
    private bool isMoving = false;
    private float moveThreshold = 0.01f;
    [Header("虚拟移动轮盘")]
    public VirtualJoystick joystick;

    private Rigidbody rb;
    private Animator animator;
    [Header("玩家音效")]
    public AudioSource moveSound;
    public AudioSource shootSound;
    [Header("按钮Btn_UI_Element")]
    public Button btn_ResetLive;
    [Header("子弹概率配置")]
    [Range(0, 100)]
    public int normalBulletChance = 100;
    [Range(0, 100)]
    public int explosiveBulletChance = 0;
    private int frostBulletChance => 100 - normalBulletChance - explosiveBulletChance;


    void Awake()
    {
        InitiatePlayerInfo();
        //UIManager.Instance.UpdateAndShowPlayerHP(currentHealth, health);
    }

    void Start()
    {
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

        // 射击（控制间隔）
        fireTimer += Time.deltaTime;
        if (Input.GetMouseButton(0) && fireTimer >= fireRate)
        {
            Shoot();
            shootSound.Play();
            fireTimer = 0f;
        }

        AnimatorFunc();
    }

    void Shoot()
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
            BulletPool.Instance.GetBullet(firePoint.position, firePoint.rotation);
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
            ExplosiveBulletPool.Instance.GetBullet(firePoint.position, firePoint.rotation);
            return;
        }

        // 冰冻子弹
        if (FrostBulletPool.Instance == null)
        {
            Debug.LogError("冰冻子弹池未初始化!请检查FrostBulletPool的Instance设置");
            return;
        }
        FrostBulletPool.Instance.GetBullet(firePoint.position, firePoint.rotation);
    }

    public void TakeDamage(int damage)
    {
        Debug.Log($"玩家受到伤害: {damage}");
        animator.SetTrigger("Hit");
        // Original Method : currentHealth -= damage;
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
        experienceToNextLevel += 50; // 每次升级需要更多经验
        //升级刷新回复血量
        currentHealth = health;
        UIManager.Instance.UpdateAndShowPlayerHP(currentHealth, health);
        UpgradeManager.Instance.ShowUpgradeOptions();
    }

    void Die()
    {
        isDead = true;
        animator.SetTrigger("Die");
        //enemyBase.playerisdied = isDead;
        Debug.Log("Player Died");
        // 这里可以添加死亡动画或效果
        GameManager.Instance.GameOver();
    }

    void AnimatorFunc()
    {
        animator.SetBool("Run", rb.velocity.magnitude > 0);
        animator.SetBool("Shoot", Input.GetMouseButton(0));
    }

    public void ResetLive() //复活功能 - 用于激励广告！提高游戏宽容度!
    {
        Debug.Log("选择复活!");
    }

    void InitiatePlayerInfo()  //游戏开始时，初始化玩家数据信息
    {
        Debug.Log("初始化玩家信息完成!True!");
        currentHealth = health;
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
                int delta = (100 - normalBulletChance - explosiveBulletChance) - newPercentage;
                explosiveBulletChance += delta;
                break;
        }
        ClampProbabilities(); // 确保修正
    }
}

public enum BulletType
{
    Normal,    // 普通子弹
    Explosive, // 爆炸子弹
    Frost      // 冰冻子弹
}