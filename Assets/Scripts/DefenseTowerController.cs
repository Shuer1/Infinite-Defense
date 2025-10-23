using System;
using System.Collections;
using UnityEngine;

public class DefenseTowerController : MonoBehaviour
{
    private static DefenseTowerController instance;
    
    [Header("Tower Attributes")]
    public int maxHealth = 500;
    public int currentHealth;
    public bool isBroken = false;
    private bool isInvincible = false;

    [SerializeField] Camera mainCamera;
    [SerializeField] PlayerController pc;

    public static DefenseTowerController Instance => instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Debug.LogWarning("多个防御塔实例存在，保留第一个实例");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        maxHealth = DataManager.GetInt(DataManager.DefenseTowerMaxHPKey);
        currentHealth = maxHealth;
        gameObject.tag = "Tower";
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0 || isInvincible || isBroken) return;

        currentHealth = Mathf.Max(currentHealth - damage, 0);
        UIManager.Instance?.UpdateAndShowTowerHP(currentHealth, maxHealth);

        if (CameraShakeController.Instance != null)
            CameraShakeController.Instance.TriggerShake();

        if (currentHealth <= 0)
            TowerBreaken();
    }

    private void TowerBreaken()
    {
        if (isBroken)
            return;

        isBroken = true;
        Debug.Log("防御塔被摧毁！调用复活管理器");

        Collider collider = GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        // ✅ 播放玩家死亡动画而非强制死亡
        if (pc != null)
            pc.Die();

        // ✅ 游戏不立即结束，等待复活选择
        GameManager.Instance.isGameOver = false;

        // ✅ 弹出复活面板
        PlayerReviveManager.Instance.OnPlayerDied();
    }

    public void ResumeTower()
    {
        isBroken = false;
        currentHealth = maxHealth;
        UIManager.Instance?.UpdateAndShowTowerHP(currentHealth, maxHealth);

        Collider collider = GetComponent<Collider>();
        if (collider != null)
            collider.enabled = true;

        GameManager.Instance.isGameOver = false;

        if (pc != null)
            pc.ResetLive();

        StartCoroutine(InvincibilityTime(5f));
    }

    private IEnumerator InvincibilityTime(float duration)
    {
        isInvincible = true;
        Debug.Log("防御塔处于无敌时间");
        yield return new WaitForSeconds(duration);
        isInvincible = false;
        Debug.Log("防御塔无敌时间结束");
    }
}
