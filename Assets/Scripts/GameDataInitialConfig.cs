// 初始值配置类（集中存放所有业务初始值）
public static class PlayerInitialConfig
{
    // 玩家初始属性
    public const int MaxHealth = 100;       // 初始最大生命值
    public const int Level = 1;             // 初始等级
    public const int NextLevelExp = 100;    // 初始下一级经验
    public const float ShootInterval = 0.3f; // 初始发射周期
    public const int CurrentPropCount = 3; // 道具数量初始值
    public const int HighScore = 0;
    public const int CurrentWave = 1;
}

public static class BulletsInitialConfig
{
    public const int BasicBulletDamage = 50;
    //-----
    public const int ExplosiveBulletDamage = 5;
    public const float ExplosionRange = 1.0f;
    //-----
    public const int FrostBulletDamage = 5;
    public const float FrostFreezeDuration = 0.5f;
    public const float FrostRadius = 1.0f;
    //-----
    public const int LightningBulletDamage = 15;
    public const int LightningCount = 2;
}

public static class BulletsChanceConfig
{
    public const int NormalBulletChance = 70;
    public const int ExplosiveBulletChance = 10;
    public const int FrostBulletChance = 10;
    public const int LightningBulletChance = 10;
}

public static class EnemysInitialConfig
{
    public const int EnemiesLevel = 1;
    //敌人1
    public const int Enemy1MaxHealth = 100;
    public const int Enemy1Damage = 20;
    public const int Enemy1ExpReward = 10;
    //敌人2
    public const int Enemy2MaxHealth = 150;
    public const int Enemy2Damage = 35;
    public const int Enemy2ExpReward = 25;
    //敌人3
    public const int Monster1MaxHealth = 200;
    public const int Monster1Damage = 50;
    public const int Monster1ExpReward = 50;

}

public static class SettingIntialConfig
{
    public const int LastestVolume = 1; //0-1
}