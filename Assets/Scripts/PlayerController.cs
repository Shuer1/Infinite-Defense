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

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = health;
    }

    void Update()
    {
        // 移动（WASD / 方向键）
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(h, 0, v) * moveSpeed;
        rb.velocity = move;

        // 旋转朝向鼠标（屏幕到射线）
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 lookDir = (hitPoint - transform.position);
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
                transform.forward = lookDir;
        }

        // 射击
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Enemy"))
        {
            GameManager.Instance.GameOver();
            Destroy(gameObject);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
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

    void Die()
    {
        Debug.Log("Player Died");
        // 这里可以添加死亡动画或效果
        GameManager.Instance.GameOver();
        Destroy(gameObject);
    }
}
