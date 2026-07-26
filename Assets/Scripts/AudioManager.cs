using UnityEngine;

// Path: Assets/Scripts/Audio/AudioManager.cs
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("拖入用于播放BGM的AudioSource")]
    public AudioSource bgmSource;
    [Tooltip("拖入用于播放音效的AudioSource")]
    public AudioSource sfxSource;

    [Header("Default Audio")]
    [Tooltip("把你准备好的背景音乐拖到这里")]
    public AudioClip defaultBGM;

    // 【新增】一个可以在面板滑动的音量条，默认设为 0.2 (20%音量)
    [Tooltip("BGM的默认音量 (0表示无声, 1表示最大)")]
    [Range(0f, 1f)]
    public float defaultBgmVolume = 0.2f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 切换场景时不销毁，保证音乐连续播放
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 游戏开始时自动播放设定的 BGM
        if (defaultBGM != null)
        {
            PlayBGM(defaultBGM);
        }
    }

    // --- 以下方法保持不变 ---
    public void SetGlobalVolume(float volume)
    {
        // 控制全局音量大小
        AudioListener.volume = Mathf.Clamp01(volume);
    }

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null || clip == null) return;

        // 如果正在播放同一首，则不重复播放
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.volume = defaultBgmVolume; // 【核心修改】播放音乐时，强制把音量压低到设定值
        bgmSource.loop = true; // 循环播放
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volumeMultiplier);
        }
    }
}