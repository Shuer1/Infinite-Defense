using UnityEngine;
using System.Collections;

public class CameraShakeController : MonoBehaviour
{
    public static CameraShakeController Instance { get; private set; }

    [Header("Camera Shake Settings")]
    public float shakeDuration = 0.2f;   // 每次震动持续时间
    public float shakeMagnitude = 0.15f; // 震动强度
    public float dampingSpeed = 1.5f;    // 平滑恢复速度

    private Vector3 originalPos;
    private bool isShaking = false;      // ✅ 防止重复触发

    private void Awake()
    {
        // 单例初始化
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 对外调用：触发摄像机晃动
    /// </summary>
    public void CameraShake()
    {
        if (isShaking) return; // ✅ 防止多次叠加
        StartCoroutine(ShakeCoroutine());
    }

    /// <summary>
    /// 摄像机晃动协程
    /// </summary>
    private IEnumerator ShakeCoroutine()
    {
        isShaking = true;
        originalPos = transform.localPosition;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 平滑恢复原位
        while (Vector3.Distance(transform.localPosition, originalPos) > 0.001f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalPos, Time.deltaTime * dampingSpeed);
            yield return null;
        }

        transform.localPosition = originalPos;
        isShaking = false;
    }
}
