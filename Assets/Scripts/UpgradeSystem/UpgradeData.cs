using UnityEngine;

[System.Serializable]
public class UpgradeData
{
    public UpgradeType upgradeType; // 升级唯一标识
    public string displayName; // 显示名称
    public string description; // 描述文本
    public Sprite cardImage; // 卡片图片
    public int value; // 升级数值
    public UpgradeType type; // 新增：关联升级类型枚举（与管理器保持一致）
}

// 统一升级类型枚举（与UpgradeManager同步，移除重复定义）
public enum UpgradeType
{
    Attack,               // 普通子弹攻击力
    FireRate,             // 玩家射速
    MaxHealth,            // 玩家最大血量
    AddChanceForExplosive,// 增加爆炸弹发射几率
    AddChanceForFrost,    // 增加冰冻弹发射几率
    AddChanceForLightning,// 增加闪电弹发射几率
    ExploseRange,         // 爆炸弹范围
    SlowTime,              // 冰冻弹减速时长
    AddLightningCount    // 增加闪电弹数量
}