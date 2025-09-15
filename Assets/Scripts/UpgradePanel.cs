using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UpgradePanel : MonoBehaviour
{
    public static UpgradePanel Instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private UpgradeCard cardPrefab;
    [SerializeField] private Button closeButton;

    private List<UpgradeCard> spawnedCards = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else if(Instance != this)
        {
            Destroy(gameObject);
        }

        closeButton.onClick.AddListener(Hide);
        Hide(); // 初始隐藏面板
    }

    /// <summary>
    /// 显示升级面板并加载卡片
    /// </summary>
    public void Show(List<UpgradeData> upgradeOptions)
    {
        ClearCards();
        panel.SetActive(true);
        Time.timeScale = 0; // 暂停游戏

        foreach (var data in upgradeOptions)
        {
            UpgradeCard card = Instantiate(cardPrefab, cardContainer);
            card.Initialize(data);
            spawnedCards.Add(card);
        }
    }

    /// <summary>
    /// 隐藏升级面板
    /// </summary>
    public void Hide()
    {
        panel.SetActive(false);
        Time.timeScale = 1; // 恢复游戏
    }

    /// <summary>
    /// 清除现有卡片（直接销毁容器内所有子物体）
    /// </summary>
    private void ClearCards()
    {
        // 从后向前遍历销毁，避免子物体索引动态变化导致漏删
        for (int i = cardContainer.childCount - 1; i >= 0; i--)
        {
            Transform childCard = cardContainer.GetChild(i);
            Destroy(childCard.gameObject);
        }
        spawnedCards.Clear(); // 同时清空记录列表，保证新卡片生成时能正确记录
    }
}
    