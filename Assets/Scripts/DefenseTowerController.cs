using System;
using System.Collections;
using UnityEngine;

public class DefenseTowerController : MonoBehaviour
{
    private static DefenseTowerController instance;
    
    [Header("Tower Attributes")]
    public int maxHealth = 500;
    public int currentHealth;
    private bool isInvincible = false;

    //private Animator animator;
    [SerializeField] Camera mainCamera;
    [SerializeField] PlayerController pc;

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
        maxHealth = DataManager.GetInt(DataManager.DefenseTowerMaxHPKey);
        //animator = GetComponent<Animator>();
        gameObject.tag = "Tower";
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0 || isInvincible) return;

        currentHealth = Mathf.Max(currentHealth - damage ,0); //防止为负
        UIManager.Instance?.UpdateAndShowTowerHP(currentHealth,maxHealth);

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

        // 不再直接设置游戏结束，而是给玩家复活的机会
        Debug.Log("防御塔被摧毁！玩家有机会复活");
        //animator?.SetTrigger("Break");

        // 禁用塔的碰撞体，防止继续被攻击
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // 通知复活管理器防御塔已被摧毁
        PlayerReviveManager.Instance?.OnPlayerDied();
    }

    public void ResumeTower() //复活逻辑：该方法在编辑器中绑定复活按钮使用
    {
        currentHealth = maxHealth;
        UIManager.Instance?.UpdateAndShowTowerHP(currentHealth,maxHealth);
        StartCoroutine(InvincibilityTime(5f));
    }

    private IEnumerator WaitOneSecond(Action onComplete) //等待时间
    {
        yield return new WaitForSeconds(1.5f);
        onComplete?.Invoke();
    }

    private IEnumerator InvincibilityTime(float duration)
    {
        isInvincible = true;
        Debug.Log("当前处于无敌时间");
        yield return new WaitForSeconds(duration);
        isInvincible = false;
        Debug.Log("无敌时间结束");
    }

}