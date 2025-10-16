using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadScene : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject helpMenu;
    [SerializeField] private AudioSource clickedSound;
    public bool isPausing = false;

    public void LoadStartUIScene()
    {
        clickedSound.Play();
        if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.LoadSceneAsync("StartGameUI",true);
        }
        else
        {
            Debug.LogError("SceneLoadManager实例不存在,无法加载场景！");
        }
    }

    public void PauseGame()
    {
        isPausing = true;
        StartCoroutine(PauseWithSlowMotion(pauseMenu, 0.5f));
    }
    
    public void ShowHelp()
    {
        isPausing = true;
        StartCoroutine(PauseWithSlowMotion(helpMenu,0.5f));
    }
    
    public IEnumerator PauseWithSlowMotion(GameObject pauseMenu, float duration = 0.5f)
    {
        float startTime = Time.unscaledTime; // 记录开始时间（用真实时间，不受timeScale影响）
        float initialTimeScale = Time.timeScale; // 记录当前时间缩放（默认1，支持从已有减速状态继续减速）

        // 渐变阶段：0.5秒内从初始值减速到0
        while (Time.unscaledTime - startTime < duration)
        {
            float t = (Time.unscaledTime - startTime) / duration; // 计算时间进度（0~1）
            Time.timeScale = Mathf.Lerp(initialTimeScale, 0, t); // 线性插值减速
            yield return null; // 等待下一帧
        }

        Time.timeScale = 0f;
        // 显示暂停菜单
        if (pauseMenu != null)
            pauseMenu.SetActive(true);
        clickedSound.Play();
    }

    // 恢复游戏
    public void ResumeGame(GameObject pauseMenu)
    {
        clickedSound.Play();
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        isPausing = false;
    }
}
