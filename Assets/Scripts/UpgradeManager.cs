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
    public int towerHPMultiFactor = 40;
    public int damageMultiFactor = 3;

    // 升级特效、音效
    [SerializeField] private ParticleSystem upgradeEffect;
    private ParticleSystem.MainModule mainModule;
    [SerializeField] private AudioSource upgradeAudio;

    // 常量定义
    private const float FIRE_RATE_REDUCTION_MULTIPLIER = 0.01f;
    private const float SLOW_DURATION_MULTIPLIER = 0.25f;
    private const float EXPLOSION_RANGE_MULTIPLIER = 0.15f;
    private const float MIN_FIRE_RATE = 0.2f;
    private const float MAX_SLOW_DURATION = 2.5f;
    private const float MAX_EXPLOSION_RANGE = 2f;
    private const int MAX_THUNDER_COUNT = 5;

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
        if (UIManager.Instance != null && (UIManager.Instance.isUpgradePanelOpen || UIManager.Instance.isProcessingUpgradePanel))
        {
            Debug.LogWarning("升级面板已打开,忽略ShowUpgradeOptions调用");
            return;
        }

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
        rewardManager?.CancelDraggingPropIfAny();

        if (upgradePanelScaleAnim != null && !GameManager.Instance.isGameOver)
        {
            CameraShakeController.Instance?.CancelShake();
            CameraShakeController.Instance.allowShake = false;
            
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

    public void ApplySelectedUpgrade(UpgradeType upgradeType)
    {
        var upgrade = allUpgrades.FirstOrDefault(u => u.upgradeType == upgradeType);
        if (upgrade == null)
        {
            Debug.LogError($"未找到升级：{upgradeType}", this);
            return;
        }

        ApplyUpgradeByType(upgrade.type, upgrade.value);

        UIManager.Instance.isProcessingUpgradePanel = false;
        UIManager.Instance.isUpgradePanelOpen = false;

        Debug.Log($"应用升级：{upgrade.displayName}(ID: {upgradeType})");
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

        int currentDamage = DataManager.GetInt(DataManager.BaseBulletDamageKey); // 获取当前普通子弹伤害
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

        dTController.maxHealth += value * towerHPMultiFactor;
        dTController.currentHealth = dTController.maxHealth;

        UIManager.Instance?.UpdateAndShowPlayerHP(playerController.currentHealth, playerController.health);
        UIManager.Instance?.UpdateAndShowTowerHP(dTController.currentHealth, dTController.maxHealth);

        DataManager.SaveInt(DataManager.PlayerMaxHealthKey, playerController.health);
        DataManager.SaveInt(DataManager.DefenseTowerMaxHPKey, dTController.maxHealth);
        Debug.Log($"玩家最大生命值提升至：{playerController.health}; 城墙最大生命值提升至：{dTController.maxHealth}");
    }

    private void ApplyExplosiveChanceUpgrade(int deltaValue) //提升爆炸子弹概率和伤害
    {
        if (BulletManager.Instance == null)
        {
            Debug.LogError("BulletManager 未初始化", this);
            return;
        }

        // ✅ 修复
        BulletManager.Instance.UpdateBulletChance(BulletType.Explosive, deltaValue);
        BulletManager.Instance.RefreshChances(); //新增

        // ✅ 伤害同步
        int currentDamage = DataManager.GetInt(DataManager.ExplosiveDamageKey);
        int newDamage = currentDamage + deltaValue * damageMultiFactor;
        BulletManager.Instance.UpdateBulletDamage(BulletType.Explosive, newDamage);
        DataManager.SaveInt(DataManager.ExplosiveDamageKey, newDamage);
    }

    private void ApplyFrostChanceUpgrade(int deltaValue) //提升冰冻子弹概率和伤害
    {
        if (BulletManager.Instance == null)
        {
            Debug.LogError("BulletManager 未初始化", this);
            return;
        }

        BulletManager.Instance.UpdateBulletChance(BulletType.Frost, deltaValue);
        BulletManager.Instance.RefreshChances(); //新增

        // ✅ 提升伤害
        int currentDamage = DataManager.GetInt(DataManager.FrostDamageKey);
        int newDamage = currentDamage + deltaValue * damageMultiFactor;
        BulletManager.Instance.UpdateBulletDamage(BulletType.Frost, newDamage);
        DataManager.SaveInt(DataManager.FrostDamageKey, newDamage);
    }
    
    private void ApplyLightningChanceUpgrade(int deltaValue) //提升闪电子弹概率和伤害
    {
        if (BulletManager.Instance == null)
        {
            Debug.LogError("BulletManager 未初始化", this);
            return;
        }

        BulletManager.Instance.UpdateBulletChance(BulletType.Lightning, deltaValue);
        BulletManager.Instance.RefreshChances(); //新增

        // ✅ 提升伤害
        int currentDamage = DataManager.GetInt(DataManager.LightningBulletDamageKey);
        int newDamage = currentDamage + deltaValue * damageMultiFactor;
        BulletManager.Instance.UpdateBulletDamage(BulletType.Lightning, newDamage);
        DataManager.SaveInt(DataManager.LightningBulletDamageKey, newDamage);
    }

    private void ApplySlowTimeLongerUpgrade(int value) //提升减速效果 实现保存✅
    {
        if (BulletManager.Instance == null)
        {
            Debug.LogError("BulletManager 未初始化", this);
            return;
        }

        float currentDuration = DataManager.GetFloat(DataManager.FrostFreezeDurationKey);
        float addDuration = value * SLOW_DURATION_MULTIPLIER;
        float newDuration = Mathf.Min(MAX_SLOW_DURATION, currentDuration + addDuration); // 限制最大时长

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

        float currentRange = DataManager.GetFloat(DataManager.ExplosionRangeKey);
        float addRange = value * EXPLOSION_RANGE_MULTIPLIER;
        float newRange = Mathf.Min(MAX_EXPLOSION_RANGE, currentRange + addRange);

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

        int currentCount = DataManager.GetInt(DataManager.LightningCountKey);
        int newCount = Mathf.Min(MAX_THUNDER_COUNT, currentCount + value);

        BulletManager.Instance.UpdateBulletSpecialValue(BulletType.Lightning, 1, newCount);

        DataManager.SaveInt(DataManager.LightningCountKey, newCount);
        Debug.Log($"闪电链接数量提升至：{newCount}");
    }
}