using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("音效源（可绑定在主摄像机或空物体上）")]
    public AudioSource lightningSource;   // 专用于闪电子弹的音效播放
    [Header("通用音效源")]
    public AudioSource sfxSource;
    [Header("事件音效表")]
    public List<EventSFXEntry> eventSFX = new List<EventSFXEntry>();
    private Dictionary<string, AudioClip> eventDict;

    [System.Serializable]
    public class EventSFXEntry
    {
        public string key;
        public AudioClip clip;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitEventDictionary();
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

    private void InitEventDictionary()
    {
        eventDict = new Dictionary<string, AudioClip>();
        foreach (var e in eventSFX)
        {
            if(!eventDict.ContainsKey(e.key) && e.clip != null)
                eventDict.Add(e.key, e.clip);
        }
    }

    public void PlayEventSFX(string key, float volume = 0.8f)
    {
        if (eventDict == null || !eventDict.ContainsKey(key)) return;

        AudioClip clip = eventDict[key];
        if (clip == null) return;

        // 若用户绑定了 2D 音源，优先使用 2D 播放
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
        else
        {
            // 兼容模式：继续用 3D 的 PlayClipAtPoint（你的原逻辑）
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
        }
    }

}
