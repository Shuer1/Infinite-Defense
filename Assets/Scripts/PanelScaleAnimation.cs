using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PanelScaleAnimation : MonoBehaviour
{
    [SerializeField] private RectTransform targetPanel;
    [SerializeField] private float animationDuration = 0.6f;
    [SerializeField] private Vector3 closedScale = Vector3.zero;  // 关闭状态缩放
    [SerializeField] private Vector3 openedScale = Vector3.one;   // 打开状态缩放
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private bool debugMode = true;

    private bool isAnimating = false;

    // 打开面板：强制从关闭状态开始动画
    public void OpenPanel()
    {
        LogDebug("尝试展开面板...");
        
        if (targetPanel == null)
        {
            Debug.LogError("PanelScaleAnimation: targetPanel 未赋值！");
            return;
        }

        // 确保面板激活
        if (!targetPanel.gameObject.activeSelf)
        {
            LogDebug("激活面板对象");
            targetPanel.gameObject.SetActive(true);
        }

        // 关键修复1：强制重置为关闭状态，确保动画有变化
        Vector3 startScale = closedScale;
        targetPanel.localScale = startScale;  // 强制从关闭状态开始
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        StopAllCoroutines();
        isAnimating = false;
        StartCoroutine(ScaleCoroutine(startScale, openedScale, true));
    }

    // 关闭面板：确保结束后重置为关闭状态
    public void ClosePanel()
    {
        LogDebug("尝试收起面板...");
        
        if (targetPanel == null)
        {
            Debug.LogError("PanelScaleAnimation: targetPanel 未赋值！");
            return;
        }

        // 关键修复2：强制从打开状态开始关闭动画
        Vector3 startScale = openedScale;
        targetPanel.localScale = startScale;  // 强制从打开状态开始
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        StopAllCoroutines();
        isAnimating = false;
        StartCoroutine(ScaleCoroutine(startScale, closedScale, false));
    }

    private IEnumerator ScaleCoroutine(Vector3 startScale, Vector3 targetScale, bool isOpening)
    {
        if (isAnimating)
        {
            LogDebug("动画已在运行，退出协程");
            yield break;
        }

        isAnimating = true;
        float elapsedTime = 0f;
        LogDebug($"动画开始: 从 {startScale} 到 {targetScale}，持续 {animationDuration} 秒");

        EnsureCanvasGroupExists();
        float startAlpha = isOpening ? 0f : 1f;  // 强制透明度起始值
        float targetAlpha = isOpening ? 1f : 0f;

        // 再次强制设置起始状态（双重保险）
        targetPanel.localScale = startScale;
        canvasGroup.alpha = startAlpha;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / animationDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            targetPanel.localScale = Vector3.Lerp(startScale, targetScale, t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            LogDebug($"动画中: t={t:F2}, scale={targetPanel.localScale}");
            yield return null;
        }

        // 关键修复3：强制设置最终状态，确保无偏差
        targetPanel.localScale = targetScale;
        canvasGroup.alpha = targetAlpha;
        isAnimating = false;
        LogDebug($"动画完成: 最终scale={targetPanel.localScale}");
    }

    private void EnsureCanvasGroupExists()
    {
        if (canvasGroup == null && targetPanel != null)
        {
            canvasGroup = targetPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = targetPanel.gameObject.AddComponent<CanvasGroup>();
                LogDebug("自动添加了CanvasGroup组件");
            }
        }
    }

    private void Start()
    {
        if (targetPanel == null)
        {
            targetPanel = GetComponent<RectTransform>();
            if (targetPanel != null)
            {
                LogDebug($"自动获取到targetPanel: {targetPanel.name}");
            }
            else
            {
                Debug.LogError("未找到RectTransform组件！");
                return;
            }
        }

        // 初始化状态：强制为关闭状态
        targetPanel.gameObject.SetActive(true);
        targetPanel.localScale = closedScale;
        EnsureCanvasGroupExists();
        canvasGroup.alpha = 0f;
        LogDebug($"初始化完成: 初始scale={closedScale}, alpha=0");
    }

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[PanelAnim] {message}");
        }
    }
}
