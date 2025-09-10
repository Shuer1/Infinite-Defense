using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void ShowUpgradeOptions()
    {
        Debug.Log("升级了! 选择一个升级选项:");
        Debug.Log("增加攻击力 / 增加血量 / 提升移动速度");
        // TODO: 在UI里展示选项，玩家选择后调用 ApplyUpgrade()
    }

    public void ApplyUpgrade(string option, PlayerController player)
    {
        switch(option)
        {
            case "Attack":
                player.damage += 5;
                break;
            case "Health":
                player.health += 20;
                player.currentHealth = player.health;
                break;
            case "Speed":
                player.fireRate -= 0.05f;
                break;
        }
    }
}
