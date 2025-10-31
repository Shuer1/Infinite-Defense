using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Tooltip("升级面板的缩放动画组件")]
    [SerializeField] private PanelScaleAnimation upgradePanelScaleAnim;

    [Tooltip("所有可用的升级选项")]
    public List<UpgradeData> allUpgrades = new List<UpgradeData>(); // 引用外部定义的UpgradeData

    [Tooltip("拖入玩家控制器实例")]
    public PlayerController playerController;

    [Tooltip("城墙血量提升倍数因子")]
    public int multiFactor = 5;

    // 升级特效、音效
    [SerializeField] private ParticleSystem upgradeEffect;
    private ParticleSystem.MainModule mainModule;
    [SerializeField] private AudioSource upgradeAudio;

    // 常量定义
    private const float FIRE_RATE_REDUCTION_MULTIPLIER = 0.01f;
    private const float SLOW_DURATION_MULTIPLIER = 0.5f;
    private const float EXPLOSION_RANGE_MULTIPLIER = 0.2f;
    private const float MIN_FIRE_RATE = 0.1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError($"重复的UpgradeManager实例，已销毁：{gameObject.name}", gameObject);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("未找到PlayerController，请手动赋值", this);
            }
        }

        if (upgradeEffect == null)
        {
            Debug.LogError("upgradeEffect未赋值", this);
            return;
        }
        mainModule = upgradeEffect.main;
        upgradeEffect.gameObject.SetActive(false);
    }

    void Update()
    {
        if (upgradeEffect.isPlaying && upgradeEffect.time >= mainModule.duration - 0.01f)
        {
            upgradeEffect.Stop();
            upgradeEffect.gameObject.SetActive(false);
        }
    }

    public void ShowUpgradeOptions()
    {
        if (allUpgrades.Count < 3)
        {
            Debug.LogError($"升级选项不足3个(当前：{allUpgrades.Count})", this);
            return;
        }

        var randomUpgrades = GetRandomUniqueUpgrades(3);

        if (UpgradePanel.Instance == null)
        {
            Debug.LogError("UpgradePanel实例不存在", this);
            return;
        }

        RewardManager rewardManager = FindObjectOfType<RewardManager>();
        if (rewardManager != null)
        {
            rewardManager.CancelDraggingPropIfAny();
        }

        if (upgradePanelScaleAnim != null && !GameManager.Instance.isGameOver)
        {
            if (CameraShakeController.Instance != null)
            {
                CameraShakeController.Instance.allowShake = false;
                CameraShakeController.Instance.CancelShake();
            }
            
            UIManager.Instance.ShowUpgradePanel();
        }
        else
        {
            Debug.LogError("upgradePanelScaleAnim未赋值 或 游戏已结束", this);
        }

        UpgradePanel.Instance.Show(randomUpgrades);
    }

    private List<UpgradeData> GetRandomUniqueUpgrades(int count)
    {
        List<UpgradeData> shuffled = new List<UpgradeData>(allUpgrades);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        return shuffled.Take(count).ToList();
    }

    public void ApplySelectedUpgrade(string upgradeId)
    {
        var upgrade = allUpgrades.FirstOrDefault(u => u.upgradeId == upgradeId);
        if (upgrade == null)
        {
            Debug.LogError($"未找到升级：{upgradeId}", this);
            return;
        }

        ApplyUpgradeByType(upgrade.type, upgrade.value);
        Debug.Log($"应用升级：{upgrade.displayName}（ID：{upgradeId}）");
    }

    private void ApplyUpgradeByType(UpgradeType type, int value) // 引用外部定义的UpgradeType
    {
        switch (type)
        {
            case UpgradeType.Attack:
                ApplyAttackUpgrade(value);
                break;
            case UpgradeType.FireRate:
                ApplyFireRateUpgrade(value);
                break;
            case UpgradeType.MaxHealth:
                ApplyMaxHealthUpgrade(value);
                break;
            case UpgradeType.AddChanceForExplosive:
                ApplyExplosiveChanceUpgrade(value);
                break;
            case UpgradeType.AddChanceForFrost:
                ApplyFrostChanceUpgrade(value);
                break;
            case UpgradeType.AddChanceForLightning:
                ApplyLightningChanceUpgrade(value);
                break;
            case UpgradeType.ExploseRange:
                ApplyBulletRangeUpgrade(value);
                break;
            case UpgradeType.SlowTime:
                ApplySlowTimeLongerUpgrade(value);
                break;
            case UpgradeType.AddLightningCount:
                ApplyAddLightningCountUpgrade(value);
                break;
            default:
                Debug.LogWarning($"未处理的升级类型：{type}", this);
                break;
        }

        if (upgradeEffect != null)
        {
            upgradeEffect.gameObject.SetActive(true);
            upgradeEffect.Play();
        }
        if (upgradeAudio != null && !upgradeAudio.isPlaying)
        {
            upgradeAudio.Play();
        }
    }

    private void ApplyAttackUpgrade(int value) //提升普通子弹伤害 实现保存✅
    {
        if (BulletManager.Instance == null)
        {
            Debug.LogError("BulletManager 未初始化", this);
            return;
        }

        int currentDamage = BulletManager.Instance.GetBulletDamage(BulletType.Normal);
        int newDamage = currentDamage + value;
        
        BulletManager.Instance.UpdateBulletDamage(BulletType.Normal, newDamage);
        UIManager.Instance?.ShowAndUpdatePlayerAttack(newDamage);

        DataManager.SaveInt(DataManager.BaseBulletDamageKey, newDamage);
        Debug.Log($"普通子弹伤害提升至：{newDamage}");
    }

    private void ApplyFireRateUpgrade(int value) //提升射速 实现保存✅
    {
        if (playerController == null)
        {
            Debug.LogError("playerController为null", this);
            return;
        }

        float reduction = value * FIRE_RATE_REDUCTION_MULTIPLIER;
        playerController.fireRate = Mathf.Max(MIN_FIRE_RATE, playerController.fireRate - reduction);

        DataManager.SaveFloat(DataManager.PlayerShootSpeedKey, playerController.fireRate, isGreater: false);
        Debug.Log($"射速提升，当前间隔：{playerController.fireRate:F2}秒");
    }

    private void ApplyMaxHealthUpgrade(int value) //提升最大生命值 实现保存✅
    {
        DefenseTowerController dTController = DefenseTowerController.Instance;
        if (playerController == null || dTController == null)
        {
            Debug.LogError("playerController和DefenseTowerController为null", this);
            return;
        }

        playerController.health += value;
        playerController.currentHealth = Mathf.Min(playerController.currentHealth + value, playerController.health);

        dTController.maxHealth += value * multiFactor;
        dTController.currentHealth = dTController.maxHealth;

        UIManager.Instance?.UpdateAndShowPlayerHP(playerController.currentHealth, playerController.health);
        UIManager.Instance?.UpdateAndShowTowerHP(dTController.currentHealth, dTController.maxHealth);

        DataManager.SaveInt(DataManager.PlayerMaxHealthKey, playerController.health);
        DataManager.SaveInt(DataManager.DefenseTowerMaxHPKey, dTController.maxHealth);
        Debug.Log($"玩家最大生命值提升至：{playerController.health}; 城墙最大生命值提升至：{dTController.maxHealth}");
    }

    private void ApplyExplosiveChanceUpgrade(int value) //提升爆炸子弹概率和伤害
    {
        if (BulletManager.Instance == null)
        {
            Debug.LogError("BulletManager 未初始化", this);
            return;
        }

        // 增加爆炸子弹概率
        BulletManager.Instance.UpdateBulletChance(BulletType.Explosive, value);
        
        // 增加爆炸子弹伤害
        int currentDamage = BulletManager.Instance.GetBulletDamage(BulletType.Explosive);
        int newDamage = currentDamage + value;
        BulletManager.Instance.UpdateBulletDamage(BulletType.Explosive, newDamage);

        DataManager.SaveInt(DataManager.ExplosiveDamageKey, newDamage);
        Debug.Log($"爆炸子弹概率提升，伤害提升至：{newDamage}");
    }

    private void ApplyFrostChanceUpgrade(int value) //提升冰冻子弹概率和伤害
    {
        if (BulletManager.Instance == null)
        {
            Debug.LogError("BulletManager 未初始化", this);
            return;
        }

        // 增加冰冻子弹概率
        BulletManager.Instance.UpdateBulletChance(BulletType.Frost, value);

        // 增加冰冻子弹伤害
        int currentDamage = BulletManager.Instance.GetBulletDamage(BulletType.Frost);
        int newDamage = currentDamage + value;
        BulletManager.Instance.UpdateBulletDamage(BulletType.Frost, newDamage);

        DataManager.SaveInt(DataManager.FrostDamageKey, newDamage);
        Debug.Log($"冰冻子弹概率提升，伤害提升至：{newDamage}");
    }
    
    private void ApplyLightningChanceUpgrade(int value) //提升闪电子弹概率和伤害
    {
        if (BulletManager.Instance == null)
        {
            Debug.LogError("BulletManager 未初始化", this);
            return;
        }

        // 添加闪电弹概率
        BulletManager.Instance.UpdateBulletChance(BulletType.Lightning, value);

        // 添加闪电弹伤害
        int currentDamage = BulletManager.Instance.GetBulletDamage(BulletType.Lightning);
        int newDamage = currentDamage + value;
        BulletManager.Instance.UpdateBulletDamage(BulletType.Lightning, newDamage);

        DataManager.SaveInt(DataManager.LightningBulletDamageKey, newDamage);
        Debug.Log($"闪电弹概率提升，伤害提升至：{newDamage}");
    }

    private void ApplySlowTimeLongerUpgrade(int value) //提升减速效果 实现保存✅
    {
        if (BulletManager.Instance == null)
        {
            Debug.LogError("BulletManager 未初始化", this);
            return;
        }

        float currentDuration = BulletManager.Instance.GetBulletSpecialValue(BulletType.Frost, 1);
        float addDuration = value * SLOW_DURATION_MULTIPLIER;
        float newDuration = currentDuration + addDuration;

        BulletManager.Instance.UpdateBulletSpecialValue(BulletType.Frost, 1, newDuration);

        DataManager.SaveFloat(DataManager.FrostFreezeDurationKey, newDuration);
        Debug.Log($"冰冻减速时长提升至：{newDuration:F1}秒");
    }

    private void ApplyBulletRangeUpgrade(int value) //提升爆炸范围 实现保存✅
    {
        if (BulletManager.Instance == null)
        {
            Debug.LogError("BulletManager 未初始化", this);
            return;
        }

        float currentRange = BulletManager.Instance.GetBulletSpecialValue(BulletType.Explosive, 1);
        float addRange = value * EXPLOSION_RANGE_MULTIPLIER;
        float newRange = currentRange + addRange;

        BulletManager.Instance.UpdateBulletSpecialValue(BulletType.Explosive, 1, newRange);

        DataManager.SaveFloat(DataManager.ExplosionRangeKey, newRange);
        Debug.Log($"爆炸范围提升至：{newRange:F1}米");
    }
    
    private void ApplyAddLightningCountUpgrade(int value) //提升闪电弹数量 实现保存✅
    {
        if (BulletManager.Instance == null)
        {
            Debug.LogError("BulletManager 未初始化", this);
            return;
        }

        int currentCount = (int)BulletManager.Instance.GetBulletSpecialValue(BulletType.Lightning, 1);
        int newCount = currentCount + value;
        BulletManager.Instance.UpdateBulletSpecialValue(BulletType.Lightning, 1, newCount);

        DataManager.SaveInt(DataManager.LightningCountKey, newCount);
    }
}