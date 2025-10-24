using UnityEngine;
using UnityEngine.UI;

public class StartSceneButtonBinder : MonoBehaviour
{
    // 在Inspector中拖入开始场景的按钮（如“开始游戏”“退出游戏”按钮）
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button exitGameButton;

    private void Start()
    {
        // 等待1帧确保SceneLoadManager单例已初始化
        Invoke(nameof(BindButtons), 0.1f);
    }

    private void BindButtons()
    {
        // 检查SceneLoadManager实例是否存在
        if (SceneLoadManager.Instance == null)
        {
            Debug.LogError("SceneLoadManager实例不存在，无法绑定按钮事件！");
            return;
        }

        // 绑定“开始游戏”按钮（假设SceneLoadManager中有加载游戏场景的方法）
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(() =>
            {
                // 替换为你的游戏场景名称
                SceneLoadManager.Instance.LoadSceneAsync("MainScene", true);
            });
        }

        // 绑定“退出游戏”按钮（假设SceneLoadManager中有退出方法）
        if (exitGameButton != null)
        {
            exitGameButton.onClick.RemoveAllListeners();
            exitGameButton.onClick.AddListener(() => 
            {
                SceneLoadManager.Instance.ExitGame();
            });
        }

        Debug.Log("开始场景按钮事件绑定完成");
    }
}
