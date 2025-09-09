using System.Threading.Tasks;
using Unity.PlasticSCM.Editor.WebApi;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Attributes")]
    public int health = 100;
    public int currentHealth;
    public int damage = 20;
    public int level = 1;
    public int experience = 0;
    public int experienceToNextLevel = 100;

    [Header("Movement & Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.3f;
    private float fireTimer;
    public float moveSpeed = 5f;
    [Header("虚拟移动轮盘")]
    public VirtualJoystick joystick;

    private Rigidbody rb;
    private Animator animator;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = health;
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            GameManager.Instance.GameOver();
            Destroy(gameObject);
        }
    }

    public async Task TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            await Die();
        }
    }

    public void GainExp(int exp)
    {
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
        UpgradeManager.Instance.ShowUpgradeOptions();
    }

    async Task Die()
    {
        isDead = true;
        animator.SetBool("Die", true);
        Debug.Log("Player Died");
        // 这里可以添加死亡动画或效果
        GameManager.Instance.GameOver();
        await Task.Delay(2000);
        gameObject.SetActive(false);
        //Destroy(gameObject);
    }

    void AnimatorFunc()
    {
        animator.SetBool("Run", rb.velocity.magnitude > 0);
        animator.SetBool("Shoot", Input.GetMouseButton(0));
    }
}
