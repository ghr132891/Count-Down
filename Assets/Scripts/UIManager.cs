using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// 路径: Assets/Scripts/UI/UIManager.cs
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels (四大面板)")]
    public GameObject mainMenuPanel;
    public GameObject deathPanel;
    public GameObject gameOverPanel;
    public GameObject inGameMenuPanel;

    [Header("Volume Controls")]
    public Slider mainMenuVolumeSlider;
    public Slider inGameVolumeSlider;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 初始化音量滑块并绑定事件
        float currentVolume = AudioListener.volume;
        if (mainMenuVolumeSlider != null)
        {
            mainMenuVolumeSlider.value = currentVolume;
            mainMenuVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
        if (inGameVolumeSlider != null)
        {
            inGameVolumeSlider.value = currentVolume;
            inGameVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // 游戏启动时，强制进入主菜单状态
        ShowMainMenu();
    }

    private void Update()
    {
        // 监听 ESC 键呼出/关闭游戏内菜单
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 如果在主菜单、死亡或失败结算界面，禁止呼出暂停菜单
            if (mainMenuPanel.activeSelf || deathPanel.activeSelf || gameOverPanel.activeSelf) return;

            if (inGameMenuPanel.activeSelf) ResumeGame();
            else ShowInGameMenu();
        }
    }

    // ================== 音量控制 ==================
    public void OnVolumeChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetGlobalVolume(value);

        // 保持主菜单和游戏内菜单的滑块刻度同步
        if (mainMenuVolumeSlider != null && mainMenuVolumeSlider.value != value) mainMenuVolumeSlider.value = value;
        if (inGameVolumeSlider != null && inGameVolumeSlider.value != value) inGameVolumeSlider.value = value;
    }

    // ================== 主菜单逻辑 ==================
    public void ShowMainMenu()
    {
        Time.timeScale = 0f; // 冻结游戏时间
        CloseAllPanels();
        mainMenuPanel.SetActive(true);
    }

    public void StartGame()
    {
        Time.timeScale = 1f; // 恢复游戏时间
        CloseAllPanels();
        // 因为 GameManager 默认在 Start 时开启了第一天，所以直接隐藏 UI 即可开始游玩
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit();
    }

    // ================== 游戏内菜单逻辑 ==================
    public void ShowInGameMenu()
    {
        Time.timeScale = 0f;
        CloseAllPanels();
        inGameMenuPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        CloseAllPanels();
    }

    public void ReturnToMainMenu()
    {
        // 最干净的做法：直接重新加载当前场景，重置所有状态
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ================== 死亡 UI 逻辑 ==================
    public void ShowDeathPanel()
    {
        Time.timeScale = 0f;
        CloseAllPanels();
        deathPanel.SetActive(true);
    }

    public void ReturnToShelterFromDeath()
    {
        Time.timeScale = 1f;
        CloseAllPanels();
        if (GameManager.Instance != null)
        {
            // 呼叫 GameManager 执行复活结算
            GameManager.Instance.ExecutePlayerRespawn();
        }
    }

    // ================== 游戏失败 UI 逻辑 ==================
    public void ShowGameOverPanel()
    {
        Time.timeScale = 0f;
        CloseAllPanels();
        gameOverPanel.SetActive(true);
    }

    // ================== 辅助方法 ==================
    private void CloseAllPanels()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (deathPanel) deathPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (inGameMenuPanel) inGameMenuPanel.SetActive(false);
    }
}