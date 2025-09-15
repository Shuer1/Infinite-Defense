using UnityEngine;

[System.Serializable]
public class UpgradeData
{
    public string upgradeId; // 升级唯一标识
    public string displayName; // 显示名称
    public string description; // 描述文本
    public Sprite cardImage; // 卡片图片
    public int value; // 升级数值
}

public enum UpgradeType
{
    Attack,        // 子弹攻击力
    FireRate,      // 攻击速度
    MaxHealth,     // 最大生命值
    MoveSpeed,     // 移动速度
    BulletSpeed,   // 子弹速度
    BulletRange    // 子弹射程
}
    