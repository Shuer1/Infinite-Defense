using System.Collections;
using UnityEngine;

public class LightIntensityAnim : MonoBehaviour
{
    [Header("目标 Light")]
    public Light[] targetLights;

    [Header("强度设置")]
    public float dayIntensity = 1f;   // 白天最大强度
    public float nightIntensity = 0f; // 夜晚最小强度

    [Header("速度控制")]
    public float daySpeed = 0.2f;     // 白天变化速度（慢）
    public float nightSpeed = 1.0f;   // 夜晚变化速度（快）
    [Tooltip("分界点：大于此值视为白天，小于为夜晚")]
    public float dayThreshold = 0.6f;

    [Header("其他设置")]
    public bool useSmoothStep = true;
    public bool affectAmbientLight = false;

    private float currentIntensity;
    private bool isGoingToNight = true;

    void Start()
    {
        if (targetLights == null || targetLights.Length == 0)
        {
            Light main = FindObjectOfType<Light>();
            if (main != null) targetLights = new Light[] { main };
        }

        currentIntensity = dayIntensity;
        SetLightsIntensity(currentIntensity);
    }

    void Update()
    {
        float target = isGoingToNight ? nightIntensity : dayIntensity;

        // 根据当前强度判断使用快还是慢速
        float speed = currentIntensity > dayThreshold ? daySpeed : nightSpeed;

        // MoveTowards 负责数值逐步逼近目标（不做 smoothStep）
        currentIntensity = Mathf.MoveTowards(currentIntensity, target, speed * Time.deltaTime);

        // 如果允许平滑视觉变化，只用于传给灯光显示，不影响逻辑判断
        float displayIntensity = useSmoothStep 
            ? Mathf.SmoothStep(0f, 1f, currentIntensity) 
            : currentIntensity;

        SetLightsIntensity(displayIntensity);

        // 数值到达目标后立即翻转（MoveTowards 会精确到达目标）
        if (Mathf.Approximately(currentIntensity, target))
            isGoingToNight = !isGoingToNight;
    }

    void SetLightsIntensity(float value)
    {
        if (targetLights != null)
        {
            foreach (var light in targetLights)
            {
                if (light != null) light.intensity = value;
            }
        }

        if (affectAmbientLight)
            RenderSettings.ambientIntensity = value;
    }
}
