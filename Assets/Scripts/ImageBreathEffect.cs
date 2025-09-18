using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ImageBreathEffect : MonoBehaviour
{
    [Tooltip("目标UI Image组件")]
    public Image targetImage;
    
    [Tooltip("最小缩放比例（基准大小）")]
    [Range(0.5f, 1f)] public float minScale = 1f;
    
    [Tooltip("最大缩放比例（放大后的大小）")]
    [Range(1f, 2f)] public float maxScale = 1.2f;
    
    [Tooltip("一个呼吸周期的时长（秒）")]
    public float breathDuration = 2f;

    private Coroutine breathCoroutine; // 协程引用，用于精准控制
    private Vector3 lastScale; // 记录禁用前的缩放，用于恢复（可选）

    private void Awake()
    {
        // 自动获取Image组件（如果未指定）
        if (targetImage == null)
            targetImage = GetComponent<Image>();
    }

    // 物体启用时自动启动呼吸效果
    private void OnEnable()
    {
        if (targetImage != null)
        {
            // 恢复禁用前的缩放（可选逻辑，根据需求开启）
            // if (lastScale != Vector3.zero)
            //     targetImage.rectTransform.localScale = lastScale;
            
            StartBreathEffect();
        }
    }

    // 物体禁用时停止协程并重置状态
    private void OnDisable()
    {
        // 记录当前缩放（可选，用于重新启用时恢复）
        if (targetImage != null)
            lastScale = targetImage.rectTransform.localScale;
            
        StopBreathEffect();
    }

    /// <summary>
    /// 启动呼吸缩放特效
    /// </summary>
    public void StartBreathEffect()
    {
        // 停止已有协程，避免叠加
        if (breathCoroutine != null)
            StopCoroutine(breathCoroutine);
            
        breathCoroutine = StartCoroutine(BreathScaleRoutine());
    }

    /// <summary>
    /// 停止呼吸缩放特效，重置为最小缩放
    /// </summary>
    public void StopBreathEffect()
    {
        if (breathCoroutine != null)
        {
            StopCoroutine(breathCoroutine);
            breathCoroutine = null;
        }
        
        // 重置为最小缩放，确保下次启用时状态一致
        if (targetImage != null)
            targetImage.rectTransform.localScale = Vector3.one * minScale;
    }

    /// <summary>
    /// 呼吸缩放的协程逻辑
    /// </summary>
    private IEnumerator BreathScaleRoutine()
    {
        if (targetImage == null) yield break;

        while (true)
        {
            // 呼吸循环：从min→max→min，完成一个周期
            float time = 0;
            while (time < breathDuration)
            {
                // 用正弦函数实现更自然的呼吸曲线（0→1→0）
                float t = (Mathf.Sin(time / breathDuration * Mathf.PI * 2) + 1) / 2;
                float currentScale = Mathf.Lerp(minScale, maxScale, t);
                targetImage.rectTransform.localScale = Vector3.one * currentScale;
                
                time += Time.deltaTime;
                yield return null;
            }
        }
    }
}
