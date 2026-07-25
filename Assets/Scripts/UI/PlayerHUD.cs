using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 路径: Assets/Scripts/UI/PlayerHUD.cs
public class PlayerHUD : MonoBehaviour
{
    [Header("Core References")]
    public PlayerController player;

    [Header("UI Sliders & Texts")]
    public Slider hpSlider;
    public Slider staminaSlider;
    public TextMeshProUGUI timerText; // 已经去掉了 hpText

    [Header("UI Optimization & Polish")]
    [Tooltip("血条和体力条发生变化时的平滑过渡速度")]
    public float sliderLerpSpeed = 10f;

    private int lastTimerSeconds = -1; // 仅保留计时器的缓存

    private void Update()
    {
        if (player != null)
        {
            UpdateHealthUI();
            UpdateStaminaUI();
        }

        UpdateTimerUI();
    }

    private void UpdateHealthUI()
    {
        if (hpSlider != null)
        {
            if (hpSlider.maxValue != player.maxHealth)
                hpSlider.maxValue = player.maxHealth;

            hpSlider.value = Mathf.Lerp(hpSlider.value, player.currentHealth, Time.deltaTime * sliderLerpSpeed);
        }
    }

    private void UpdateStaminaUI()
    {
        if (staminaSlider != null)
        {
            if (staminaSlider.maxValue != player.maxStamina)
                staminaSlider.maxValue = player.maxStamina;

            staminaSlider.value = Mathf.Lerp(staminaSlider.value, player.currentStamina, Time.deltaTime * sliderLerpSpeed);
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            if (GameManager.Instance != null && GameManager.Instance.isDayActive)
            {
                if (GameManager.Instance.isTimerRunning)
                {
                    int currentSeconds = Mathf.FloorToInt(GameManager.Instance.currentTime);
                    if (currentSeconds != lastTimerSeconds)
                    {
                        int minutes = currentSeconds / 60;
                        int seconds = currentSeconds % 60;

                        if (GameManager.Instance.currentTime < 60) timerText.color = Color.red;
                        else timerText.color = Color.white;

                        timerText.text = $"Time Left: {minutes:00}:{seconds:00}";
                        lastTimerSeconds = currentSeconds;
                    }
                }
                else
                {
                    if (lastTimerSeconds != -2)
                    {
                        timerText.text = "Waiting to leave (Timer not started)";
                        timerText.color = Color.black;
                        lastTimerSeconds = -2;
                    }
                }
            }
            else
            {
                if (lastTimerSeconds != -3)
                {
                    timerText.text = "Time stopped / Summarizing";
                    timerText.color = Color.yellow;
                    lastTimerSeconds = -3;
                }
            }
        }
    }
}