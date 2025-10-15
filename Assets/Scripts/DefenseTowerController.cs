using System;
using System.Collections;
using UnityEngine;

public class DefenseTowerController : MonoBehaviour
{
    [Header("Tower Attributes")]
    public int maxHealth = 500;
    public int currentHealth;

    //private Animator animator;
    [SerializeField] Camera mainCamera;
    [SerializeField] PlayerController pc;
    private static DefenseTowerController instance;

    // 提供静态访问点，方便敌人查找
    public static DefenseTowerController Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Debug.LogWarning("多个防御塔实例存在，保留第一个实例");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        //animator = GetComponent<Animator>();
        gameObject.tag = "Tower";
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damage;

        Debug.Log($"防御塔受到 {damage} 点伤害，剩余生命值: {currentHealth}");

        // 触发受击动画
        if (CameraShakeController.Instance != null) CameraShakeController.Instance.TriggerShake();

        if (currentHealth <= 0)
        {
            TowerBreaken();
        }
    }

    private void TowerBreaken()
    {
        if (GameManager.Instance.isGameOver)
        {
            return;
        }

        GameManager.Instance.isGameOver = true;

        Debug.Log("防御塔被摧毁！游戏失败");
        //animator?.SetTrigger("Break");

        // 禁用塔的碰撞体，防止继续被攻击
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // 调用游戏管理器的游戏结束方法
        StartCoroutine(WaitOneSecond(() =>
        {
            GameManager.Instance?.GameOver();
        }));
        //GameManager.Instance?.GameOver();
        
    }

    public void ResumeTower() //编辑器中点击复活按钮调用
    {
        currentHealth = maxHealth;
    }

    private IEnumerator WaitOneSecond(Action onComplete) //等待时间
    {
        yield return new WaitForSeconds(1.5f);
        onComplete?.Invoke();
    }

}