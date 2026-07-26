using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Path: Assets/Scripts/UI/UIManager.cs
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject deathPanel;
    public GameObject gameOverPanel;
    public GameObject inGameMenuPanel;
    public GameObject victoryPanel; // 【新增】通关界面面板

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

        ShowMainMenu(); // 依据你原有的逻辑保持不变
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (mainMenuPanel.activeSelf || deathPanel.activeSelf || gameOverPanel.activeSelf || (victoryPanel != null && victoryPanel.activeSelf)) return;

            if (inGameMenuPanel.activeSelf) ResumeGame();
            else ShowInGameMenu();
        }
    }

    public void OnVolumeChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetGlobalVolume(value);
        if (mainMenuVolumeSlider != null && mainMenuVolumeSlider.value != value) mainMenuVolumeSlider.value = value;
        if (inGameVolumeSlider != null && inGameVolumeSlider.value != value) inGameVolumeSlider.value = value;
    }

    public void ShowMainMenu()
    {
        Time.timeScale = 0f;
        CloseAllPanels();
        mainMenuPanel.SetActive(true);
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        CloseAllPanels();
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit();
    }

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
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

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
            GameManager.Instance.ExecutePlayerRespawn();
        }
    }

    public void ShowGameOverPanel()
    {
        Time.timeScale = 0f;
        CloseAllPanels();
        gameOverPanel.SetActive(true);
    }

    // 【新增】显示通关界面的方法
    public void ShowVictoryPanel()
    {
        Time.timeScale = 0f; // 暂停游戏
        CloseAllPanels();
        if (victoryPanel != null) victoryPanel.SetActive(true);
    }

    private void CloseAllPanels()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (deathPanel) deathPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (inGameMenuPanel) inGameMenuPanel.SetActive(false);
        if (victoryPanel) victoryPanel.SetActive(false); // 【新增】关闭面板时也关闭通关界面
    }
}