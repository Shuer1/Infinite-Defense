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
            SceneLoadManager.Instance.LoadSceneAsync("StartGameUI", true, true);
        }
        else
        {
            Debug.LogError("SceneLoadManager实例不存在,无法加载场景！");
        }
    }

    public void PauseGame()
    {
        isPausing = true;
        clickedSound.Play();

        // ✅ 取消慢动作动画，改为点击立即显示菜单
        // StartCoroutine(PauseWithSlowMotion(pauseMenu, 0.5f));

        Time.timeScale = 0f;
        if (pauseMenu != null)
            pauseMenu.SetActive(true);
    }

    public void ShowHelp()
    {
        isPausing = true;
        clickedSound.Play();

        Time.timeScale = 0f;
        if (helpMenu != null)
            helpMenu.SetActive(true);
    }

    // 恢复游戏
    public void ResumeGame(GameObject pauseMenu)
    {
        clickedSound.Play();
        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        Time.timeScale = 1f;
        isPausing = false;
    }
}
