using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("音效源（可绑定在主摄像机或空物体上）")]
    public AudioSource lightningSource;   // 专用于闪电子弹的音效播放

    private void Awake()
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
    }

    public void PlayLightningSound(AudioClip clip, float volume = 1f)
    {
        if (clip == null || lightningSource == null) return;
        lightningSource.PlayOneShot(clip, volume);
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
    }

}
