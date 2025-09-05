using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float moveSpeed = 3f;
    private Transform player;
    private Animator animator;
    private bool isDead = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        animator = GetComponent<Animator>();

        // 初始进入 Run 状态
        animator.SetBool("Run", true);
    }

    void Update()
    {
        if (isDead) return; // 死亡时不再移动

        if (player != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
            transform.LookAt(player);

        }
    }

    // 示例：调用这个方法让敌人死亡
    public void Die()
    {
        isDead = true;
        animator.SetBool("Run", false);
        animator.SetBool("Die", true);

        // 你可以在这里加延迟销毁
        Destroy(gameObject, 2f);
    }
    
}
