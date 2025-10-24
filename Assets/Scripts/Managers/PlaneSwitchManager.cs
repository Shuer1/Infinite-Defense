using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneSwitchManager : MonoBehaviour
{
    [Tooltip("Plane上的材质（建议使用实例化材质，避免影响其他物体）")]
    [SerializeField] private Material targetMaterial;

    // 2. 接收Sprite资源（用于提取纹理）
    [Tooltip("用于切换的Sprite资源列表（其texture将作为Plane的贴图）")]
    [SerializeField] private Sprite[] spriteTextures;

    // Plane的渲染组件（3D专用）
    private MeshRenderer planeRenderer;

    private void Awake()
    {
        // 获取Plane的MeshRenderer组件（必选）
        planeRenderer = GetComponent<MeshRenderer>();
        if (planeRenderer == null)
        {
            Debug.LogError("当前物体不是Plane或未挂载MeshRenderer组件！");
            return;
        }

        // 自动获取材质（若未手动指定）
        if (targetMaterial == null)
        {
            // 使用实例化材质（material）而非共享材质（sharedMaterial），避免影响其他物体
            targetMaterial = planeRenderer.material;
        }
    }
    
    private void Start()
    {
        int randomSpriteOrder = Random.Range(0, spriteTextures.Length);
        SwitchToTexture(randomSpriteOrder);
    }

    /// <summary>
    /// 通过索引切换Plane的贴图（从spriteTextures数组中选择）
    /// </summary>
    public void SwitchToTexture(int index)
    {
        if (planeRenderer == null || targetMaterial == null) return;

        // 索引容错
        if (index < 0 || index >= spriteTextures.Length)
        {
            Debug.LogWarning($"无效索引：{index}，超出Sprite数组范围");
            return;
        }

        SwitchToTexture(spriteTextures[index]);
    }

    /// <summary>
    /// 核心方法：用Sprite的纹理替换Plane材质的主贴图
    /// </summary>
    public void SwitchToTexture(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogError("传入的Sprite为空！");
            return;
        }

        // 从Sprite中提取原始纹理（Texture2D）
        Texture2D newTexture = sprite.texture;
        if (newTexture == null)
        {
            Debug.LogError($"Sprite {sprite.name} 没有关联的纹理！");
            return;
        }

        // 替换材质的主贴图（_MainTex是绝大多数3D Shader的主纹理属性）
        targetMaterial.SetTexture("_MainTex", newTexture);
        Debug.Log($"Plane贴图已切换为：{sprite.name}");
    }
}
