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
    [Header("虚拟移动轮盘")]
    public VirtualJoystick joystick;

    private Rigidbody rb;
    private Animator animator;
    private UIManager uiManager;

    void Awake()
    {
        uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("UIManager is null");
            return;
        }

        currentHealth = health;
        uiManager.UpdateAndShowPlayerHP(currentHealth,health);
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

        // 射击（控制间隔）
        fireTimer += Time.deltaTime;
        if (Input.GetMouseButton(0) && fireTimer >= fireRate)
        {
            Shoot();
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
        uiManager.UpdateAndShowPlayerHP(currentHealth,health);
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
        currentHealth = health;
        uiManager.UpdateAndShowPlayerHP(currentHealth,health);
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
