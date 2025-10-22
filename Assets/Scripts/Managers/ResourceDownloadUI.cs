using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceDownloadUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject downloadPanel;
    public Slider progressBar;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI progressText;

    private void Awake()
    {
        // 确保面板初始状态是激活的
        if (downloadPanel != null)
            downloadPanel.SetActive(true);
    }

    private void OnEnable()
    {
        // 订阅进度更新事件
        InitialRemoteFetch.OnProgressUpdate += OnDownloadProgressUpdate;
    }

    private void OnDisable()
    {
        // 取消订阅进度更新事件
        InitialRemoteFetch.OnProgressUpdate -= OnDownloadProgressUpdate;
    }

    private void OnDownloadProgressUpdate(float progress, string message)
    {
        // 如果进度为负数，表示出现错误
        if (progress < 0)
        {
            HandleDownloadError(message);
            return;
        }

        // 更新进度条和文本
        if (progressBar != null)
        {
            progressBar.value = progress * 100; // 转换为0-100范围
        }

        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        if (statusText != null)
        {
            statusText.text = message;
        }

        // 如果进度达到100%，隐藏面板
        if (progress >= 1.0f)
        {
            HideDownloadPanel();
        }
    }

    private void HandleDownloadError(string errorMessage)
    {
        if (statusText != null)
        {
            statusText.text = $"错误: {errorMessage}";
        }

        if (progressText != null)
        {
            progressText.text = "失败";
        }

        // 可以在这里添加重试按钮或退出游戏的逻辑
        Debug.LogError($"资源下载失败: {errorMessage}");
    }

    private void HideDownloadPanel()
    {
        if (downloadPanel != null)
        {
            downloadPanel.SetActive(false);
        }
    }
}