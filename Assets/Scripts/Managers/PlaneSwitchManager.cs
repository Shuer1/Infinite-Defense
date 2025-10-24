using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneSwitchManager : MonoBehaviour
{
    [SerializeField] private Material plane_Material;
    [Tooltip("自定义待切换的材质集合")]
    [SerializeField] private Sprite[] plane_SpriteTextures;
    private MeshRenderer plane_Renderer;
    // Start is called before the first frame update
    void Start()
    {
        plane_Renderer = GetComponent<MeshRenderer>();
        if (plane_Renderer == null)
        {
            Debug.LogWarning("未找到 PlaneRenderer");
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    private void SwitchToTexture(int index)
    {
        if (index < 0 || index >= plane_SpriteTextures.Length)
        {
            Debug.LogWarning("索引超出范围");
            return;
        }
        plane_Renderer.material.mainTexture = plane_SpriteTextures[index].texture;
    }
}
