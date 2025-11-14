using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCard : MonoBehaviour
{
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button selectButton;

    private UpgradeType upgradeType;
    private const string MorePowerKey = "MorePower";

    private void Awake()
    {
        // 安全校验：避免重复添加监听
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnCardSelected);
    }

    /// <summary>
    /// 初始化卡片数据
    /// </summary>
    public void Initialize(UpgradeData data)
    {
        if (data == null)
        {
            Debug.LogError("初始化升级卡片失败：UpgradeData为null", this);
            return;
        }
        
        upgradeType = data.upgradeType;
        cardImage.sprite = data.cardImage;
        titleText.text = data.displayName;
        descriptionText.text = data.description;
    }

    /// <summary>
    /// 卡片被选中时的回调
    /// </summary>
    private void OnCardSelected()
    {
        if (UpgradeManager.Instance == null)
        {
            Debug.LogError("UpgradeManager 单例为 null!");
            return;
        }
        UpgradeManager.Instance.ApplySelectedUpgrade(upgradeType);
        
        if (UpgradePanel.Instance != null)
        {
            UpgradePanel.Instance.Hide();
            SoundManager.Instance.PlayEventSFX(MorePowerKey);
        }
    }
}