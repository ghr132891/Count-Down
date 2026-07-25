using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 路径: Assets/Scripts/UI/PlayerHUD.cs
public class PlayerHUD : MonoBehaviour
{
    [Header("玩家引用")]
    public PlayerController player;

    [Header("UI 引用")]
    public Slider hpSlider;
    public Slider staminaSlider;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI timerText;

    private void Update()
    {
        // 1. 刷新玩家血量与体力
        if (player != null)
        {
            if (hpSlider != null)
            {
                hpSlider.maxValue = player.maxHealth;
                hpSlider.value = player.currentHealth;
            }

            if (staminaSlider != null)
            {
                staminaSlider.maxValue = player.maxStamina;
                staminaSlider.value = player.currentStamina;
            }

            if (hpText != null) hpText.text = $"{Mathf.RoundToInt(player.currentHealth)} / {player.maxHealth}";
        }

        // 2. 刷新 8 分钟倒计时状态
        if (timerText != null)
        {
            if (GameManager.Instance != null && GameManager.Instance.isDayActive)
            {
                // 检测计时器是否已经启动
                if (GameManager.Instance.isTimerRunning)
                {
                    int minutes = Mathf.FloorToInt(GameManager.Instance.currentTime / 60);
                    int seconds = Mathf.FloorToInt(GameManager.Instance.currentTime % 60);

                    if (GameManager.Instance.currentTime < 60) timerText.color = Color.red;
                    else timerText.color = Color.white;

                    timerText.text = $"Time Left: {minutes:00}:{seconds:00}";
                }
                else
                {
                    // 白天已经开始，但玩家还在家里摸鱼没出门
                    timerText.text = "Waiting to leave (Timer not started)";
                    timerText.color = Color.black;
                }
            }
            else
            {
                timerText.text = "Time stopped / Summarizing";
                timerText.color = Color.yellow;
            }
        }
    }
}