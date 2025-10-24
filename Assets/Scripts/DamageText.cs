using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DamageText : MonoBehaviour
{
    [Header("Text Settings")]
    public TMP_Text damageText;
    public float defaultFontSize = 32f;
    public float moveSpeed = 1f;
    public float fadeDuration = 1f;
    public float spread = 2f;
    
    private Vector3 startPosition;
    private Color startColor;
    private RectTransform rectTransform;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (damageText == null)
            damageText = GetComponentInChildren<TMP_Text>();
    }
    
    /// <summary>
    /// 初始化伤害数字
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="worldPosition">世界坐标位置</param>
    public void Initialize(int damage, Vector3 worldPosition)
    {
        if (damageText != null)
        {
            damageText.text = damage.ToString();
            // 设置为红色
            damageText.color = Color.red;
            damageText.fontSize = defaultFontSize;
        }
        
        // 设置初始位置
        startPosition = worldPosition + new Vector3(
            Random.Range(-spread, spread), 
            Random.Range(-spread, spread), 
            0);
        
        if (rectTransform != null)
        {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(startPosition);
            // 确保伤害数字在屏幕范围内
            screenPosition.x = Mathf.Clamp(screenPosition.x, 50, Screen.width - 50);
            screenPosition.y = Mathf.Clamp(screenPosition.y, 50, Screen.height - 50);
            rectTransform.position = screenPosition;
        }
        
        // 保存初始颜色
        startColor = damageText != null ? damageText.color : Color.white;
        
        // 开始动画
        StartCoroutine(AnimateAndDestroy());
    }
    
    /// <summary>
    /// 执行动画并销毁
    /// </summary>
    private IEnumerator AnimateAndDestroy()
    {
        float elapsedTime = 0f;
        Vector3 startPosition = rectTransform.position;
        Vector3 targetPosition = startPosition + Vector3.up * 50f; // 向上移动
        
        while (elapsedTime < fadeDuration)
        {
            // 移动
            if (rectTransform != null)
            {
                rectTransform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / fadeDuration);
            }
            
            // 淡出
            if (damageText != null)
            {
                Color newColor = startColor;
                newColor.a = 1f - (elapsedTime / fadeDuration);
                damageText.color = newColor;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 销毁对象
        Destroy(gameObject);
    }
}