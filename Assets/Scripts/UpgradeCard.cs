using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class UpgradeCard : MonoBehaviour
{
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button selectButton;

    private string upgradeId;

    private void Awake()
    {
        selectButton.onClick.AddListener(OnCardSelected);
    }

    /// <summary>
    /// 初始化卡片数据
    /// </summary>
    public void Initialize(UpgradeData data)
    {
        upgradeId = data.upgradeId;
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
        UpgradeManager.Instance.ApplySelectedUpgrade(upgradeId);
        
        if (UpgradePanel.Instance != null)
        {
            UpgradePanel.Instance.Hide();
        }
        else
        {
            Debug.LogError("UpgradePanel 单例为 null!");
        }
    }
}
    