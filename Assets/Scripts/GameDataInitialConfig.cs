// 初始值配置类（集中存放所有业务初始值）
public static class PlayerInitialConfig
{
    // 玩家初始属性
    public const int MaxHealth = 100;       // 初始最大生命值
    public const int Level = 1;             // 初始等级
    public const int NextLevelExp = 100;    // 初始下一级经验
    public const float ShootInterval = 0.3f; // 初始发射周期
}

public static class BulletsInitialConfig
{
    public const int BasicBulletDamage = 50;
    //-----
    public const int ExplosiveBulletDamage = 55;
    public const float ExplosionRange = 1.0f;
    //-----
    public const int FrostBulletDamage = 45;
    public const float FrostFreezeDuration = 0.5f;
    public const float FrostRadius = 0.5f;
}

public static class BulletsChanceConfig
{
    public const int NormalBulletChance = 100;
    public const int ExplosiveBulletChance = 0;
}

public static class EnemysInitialConfig
{
    public const int Enemy1MaxHealth = 150;
    public const int Enemy1Damage = 25;
    public const int Enemy2MaxHealth = 200;
    public const int Enemy2Damage = 40;
}