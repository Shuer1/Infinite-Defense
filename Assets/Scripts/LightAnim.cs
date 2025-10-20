using System.Collections;
using UnityEngine;

public class LightAnim : MonoBehaviour
{
    private Light towerLight;
    public DefenseTowerController towerController;

    [Tooltip("最小光照范围")]
    [Range(0.5f, 3.0f)] public float minRange = 0.5f;
    
    [Tooltip("最大光照范围")]
    [Range(0.5f, 5.0f)] public float maxRange = 3.0f;

    [Tooltip("最小光照强度")]
    [Range(0.1f, 5.0f)] public float minIntensity = 0.5f;
    
    [Tooltip("最大光照强度")]
    [Range(0.1f, 10.0f)] public float maxIntensity = 2.0f;

    [Tooltip("一个呼吸周期的时长（秒）")]
    public float breathDuration = 2f;

    // 新增：存储原始颜色
    private Color originalLightColor;
    // 新增：危险状态颜色
    [Tooltip("生命值过低时的光源颜色")]
    public Color dangerColor = Color.red;

    void Start()
    {
        towerLight = GetComponent<Light>();
        // 存储原始颜色
        originalLightColor = towerLight.color;

        if (towerLight != null)
        {
            StartCoroutine(BreathScaleRoutine());
        }
        else
        {
            Debug.LogError("当前物体上没有Light组件！");
        }

        // 新增：如果没有找到防御塔控制器，打印警告（不影响原有功能）
        if (towerController == null)
        {
            Debug.LogWarning("未找到DefenseTowerController组件，颜色变化功能将失效", this);
        }
    }

    private IEnumerator BreathScaleRoutine()
    {
        while (true)
        {
            float time = 0;
            while (time < breathDuration)
            {
                // 计算呼吸曲线（正弦曲线映射到0-1范围）
                float t = (Mathf.Sin((time / breathDuration) * Mathf.PI * 2 - Mathf.PI / 2) + 1) / 2;
                
                // 计算当前范围和强度（根据曲线插值）
                float currentRange = Mathf.Lerp(minRange, maxRange, t);
                float currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, t);

                // 应用到光源
                towerLight.range = currentRange;
                towerLight.intensity = currentIntensity;

                // 新增：颜色切换逻辑（最小化入侵，仅添加此段代码）
                UpdateLightColorByHealth();

                time += Time.deltaTime;
                yield return null;
            }
        }
    }

    // 新增：根据生命值更新光源颜色的独立方法
    private void UpdateLightColorByHealth()
    {
        // 检查控制器是否存在且生命值有效
        if (towerController != null && towerController.maxHealth > 0)
        {
            // 判断当前生命值是否小于最大生命值的1/3
            if (towerController.currentHealth < towerController.maxHealth / 2)
            {
                towerLight.color = dangerColor;
            }
            else
            {
                // 恢复原始颜色
                towerLight.color = originalLightColor;
            }
        }
    }
}