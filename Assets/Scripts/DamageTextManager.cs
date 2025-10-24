using UnityEngine;
using UnityEngine.UI;

public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }
    
    [Header("Damage Text Settings")]
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private Canvas canvas;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        // 如果没有指定Canvas，尝试获取或创建一个
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("DamageTextCanvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }
        }
    }
    
    /// <summary>
    /// 显示伤害数字
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="worldPosition">世界坐标位置</param>
    public void ShowDamageText(int damage, Vector3 worldPosition)
    {
        if (damageTextPrefab != null && canvas != null)
        {
            GameObject damageTextObject = Instantiate(damageTextPrefab, canvas.transform);
            DamageText damageText = damageTextObject.GetComponent<DamageText>();
            if (damageText != null)
            {
                damageText.Initialize(damage, worldPosition);
            }
        }
    }
}