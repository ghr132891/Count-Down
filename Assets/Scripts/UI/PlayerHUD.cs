using UnityEngine;
using UnityEngine.UI;
using TMPro; // [新增] 引入 TextMeshPro 命名空间

// 路径: Assets/Scripts/UI/PlayerHUD.cs
public class PlayerHUD : MonoBehaviour
{
    public PlayerController player;

    [Header("UI References")]
    public Slider hpSlider;
    public Slider staminaSlider;
    public TextMeshProUGUI hpText; // [修改] 从 Text 替换为 TextMeshProUGUI

    private void Update()
    {
        if (player == null) return;

        // 实时更新血量和体力条
        hpSlider.maxValue = player.maxHealth;
        hpSlider.value = player.currentHealth;

        if (hpText != null)
            hpText.text = $"{Mathf.RoundToInt(player.currentHealth)} / {player.maxHealth}";

        staminaSlider.maxValue = player.maxStamina;
        staminaSlider.value = player.currentStamina;
    }
}