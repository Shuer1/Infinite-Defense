using UnityEngine;
using TMPro;
using System.Collections;

public class DamageText : MonoBehaviour
{
    [Header("Text Settings")]
    public TMP_Text damageText;
    public float defaultFontSize = 32f;
    public float moveDistance = 50f;
    public float fadeDuration = 1f;
    public float spread = 2f;

    private Vector3 startPosition;
    private Color startColor;
    private RectTransform rectTransform;
    bool isAnimating = false;

    // 新增字段：当前正在播放的协程
    private Coroutine currentAnimation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (damageText == null)
            damageText = GetComponentInChildren<TMP_Text>();
    }

    /// <summary>
    /// 初始化伤害数字并播放动画
    /// </summary>
    public void Initialize(int damage, Vector3 worldPosition)
    {
        // 停止当前协程，防止冲突
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }

        isAnimating = true;

        if (damageText != null)
        {
            damageText.text = damage.ToString();
            damageText.color = Color.red;
            damageText.fontSize = defaultFontSize;
        }

        // 随机偏移位置（防止重叠）
        startPosition = worldPosition + new Vector3(
            Random.Range(-spread, spread),
            Random.Range(-spread, spread),
            0f
        );

        if (rectTransform != null)
        {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(startPosition);
            screenPosition.x = Mathf.Clamp(screenPosition.x, 50, Screen.width - 50);
            screenPosition.y = Mathf.Clamp(screenPosition.y, 50, Screen.height - 50);
            rectTransform.position = screenPosition;
        }

        startColor = damageText != null ? damageText.color : Color.white;

        // 启动动画协程（独立管理）
        currentAnimation = StartCoroutine(AnimateAndRecycle());
    }

    /// <summary>
    /// 动画播放并回收到对象池
    /// </summary>
    private IEnumerator AnimateAndRecycle()
    {
        float elapsedTime = 0f;
        Vector3 start = rectTransform.position;
        Vector3 target = start + Vector3.up * moveDistance;

        while (elapsedTime < fadeDuration)
        {
            // 使用 unscaledDeltaTime，确保动画不受 Time.timeScale 影响
            float t = elapsedTime / fadeDuration;

            // 移动
            if (rectTransform != null)
                rectTransform.position = Vector3.Lerp(start, target, t);

            // 渐隐
            if (damageText != null)
            {
                Color c = startColor;
                c.a = 1f - t;
                damageText.color = c;
            }

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        Recycle();
    }

    /// <summary>
    /// 回收该对象（交给对象池）
    /// </summary>
    private void Recycle()
    {
        isAnimating = false;
        currentAnimation = null;

        // 重置透明度
        if (damageText != null)
        {
            Color c = damageText.color;
            c.a = 1f;
            damageText.color = c;
        }

        // 隐藏自身（不销毁）
        gameObject.SetActive(false);

        // 归还到对象池
        DamageTextManager.Instance?.ReturnToPool(this);
    }
}
