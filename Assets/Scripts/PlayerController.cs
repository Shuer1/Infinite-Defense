using System.Threading.Tasks;
using Unity.PlasticSCM.Editor.WebApi;
using UnityEditor.Scripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Attributes")]
    public int health = 100;
    public int currentHealth = 100;
    public int damage;
    public int level = 1;
    public int experience = 0;
    public int experienceToNextLevel = 100;

    [Header("Movement & Shooting")]
    public GameObject bulletPrefab;
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

    void Awake()
    {
        currentHealth = health;
        UIManager.Instance.UpdateAndShowPlayerHP(currentHealth,health);
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

#if UNITY_ANDROID || UNITY_IOS
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
        BulletPool.Instance.GetBullet(firePoint.position, firePoint.rotation);
    }

    public void TakeDamage(int damage)
    {
        Debug.Log($"玩家受到伤害: {damage}");
        animator.SetTrigger("Hit");
        // Original Method : currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth - damage, 0);
        UIManager.Instance.UpdateAndShowPlayerHP(currentHealth,health);
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
    }

    void LevelUp()
    {
        level++;
        experience = 0;
        experienceToNextLevel += 50; // 每次升级需要更多经验
        //升级刷新回复血量
        currentHealth = health;
        UIManager.Instance.UpdateAndShowPlayerHP(currentHealth,health);

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

    public void ResetToLive() //复活功能 - 用于激励广告！提高游戏宽容度！
    {

    }

    void InitiatePlayerInfo()  //游戏开始时，初始化玩家数据信息
    {
        
    }
}
