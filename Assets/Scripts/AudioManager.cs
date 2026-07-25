using UnityEngine;

// 路径: Assets/Scripts/Audio/AudioManager.cs
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("用于播放循环的背景音乐")]
    public AudioSource bgmSource;
    [Tooltip("用于播放一次性的音效")]
    public AudioSource sfxSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 保证切换场景时音乐不会中断
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- 【全局音量调节接口】 ---
    public void SetGlobalVolume(float volume)
    {
        // 直接控制 Unity 全局音量，0 为静音，1 为最大声
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    // --- 【背景音乐接口】 ---
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;

        // 避免重复播放同一首歌
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    // --- 【音效接口】 (为你后续开发预留) ---
    // 其他脚本只需调用 AudioManager.Instance.PlaySFX(你的音效文件) 即可
    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volumeMultiplier);
        }
    }
}