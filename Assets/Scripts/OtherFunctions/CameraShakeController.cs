using UnityEngine;
using System.Collections;

public class CameraShakeController : MonoBehaviour
{
    public static CameraShakeController Instance { get; private set; }
    public bool allowShake = true;

    [Header("抖动参数")]
    public float shakeDuration = 0.2f; // 抖动持续时间
    public float shakeMagnitude = 0.15f; // 抖动幅度
    public float dampingSpeed = 1.5f; // 恢复速度

    private bool isShaking = false;
    private CameraFollow cameraFollow; // 引用跟随脚本

    private void Awake()
    {
        // 单例初始化
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 获取跟随脚本（确保和摄像机挂载在同一物体）
        cameraFollow = GetComponent<CameraFollow>();
    }

    // 外部调用：触发抖动
    public void TriggerShake()
    {
        if (!allowShake) return;

        if (!isShaking && cameraFollow != null)
        {
            StartCoroutine(ShakeCoroutine());
        }
    }

    public void CancelShake()
    {
        StopAllCoroutines();
        if (cameraFollow != null)
            cameraFollow.transform.localPosition = Vector3.zero;
            
        isShaking = false;
    }

    private IEnumerator ShakeCoroutine()
    {
        isShaking = true;
        float elapsed = 0f;

        // 第一阶段：随机抖动（只修改偏移量）
        while (elapsed < shakeDuration)
        {
            // 生成随机偏移（在X、Y、Z轴上叠加微小抖动）
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            float z = Random.Range(-1f, 1f) * shakeMagnitude;

            // 直接设置抖动偏移量（跟随脚本会自动叠加到基础位置）
            cameraFollow.shakeOffset = new Vector3(x, y, z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 第二阶段：平滑恢复（将偏移量归零）
        while (cameraFollow.shakeOffset.sqrMagnitude > 0.001f) // 用平方长度判断，性能更好
        {
            // 逐渐减小偏移量至0
            cameraFollow.shakeOffset = Vector3.Lerp(
                cameraFollow.shakeOffset, 
                Vector3.zero, 
                Time.deltaTime * dampingSpeed
            );
            yield return null;
        }

        // 确保偏移量完全归零
        cameraFollow.shakeOffset = Vector3.zero;
        isShaking = false; // 允许下次抖动
    }
}