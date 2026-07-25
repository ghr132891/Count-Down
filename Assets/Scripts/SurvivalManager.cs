using UnityEngine;

// Path: Assets/Scripts/SurvivalManager.cs
public class SurvivalManager : MonoBehaviour
{
    public static SurvivalManager Instance { get; private set; }

    [Header("Global Core Stats")]
    // 变更为三个核心数值
    public float totalFoodValue = 100f;
    public float totalWaterValue = 100f;
    public float totalDurabilityValue = 100f;

    [Header("Daily Deduction Settings")]
    public float baseFoodDeduction = 5f;
    public float baseWaterDeduction = 5f;
    public float baseDurabilityDeduction = 5f;
    public float dayMultiplier = 2f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddValues(float foodVal, float waterVal, float durVal)
    {
        totalFoodValue += foodVal;
        totalWaterValue += waterVal;
        totalDurabilityValue += durVal;
    }

    public void ProcessDailyDeduction(int day, out float foodDed, out float waterDed, out float durDed)
    {
        foodDed = 0f;
        waterDed = 0f;
        durDed = 0f;
        if (day == 1) return;

        foodDed = baseFoodDeduction + (day * dayMultiplier);
        waterDed = baseWaterDeduction + (day * dayMultiplier);
        durDed = baseDurabilityDeduction + (day * dayMultiplier);

        totalFoodValue -= foodDed;
        totalWaterValue -= waterDed;
        totalDurabilityValue -= durDed;

        Debug.Log($"Day {day} deduction: Food -{foodDed}, Water -{waterDed}, Durability -{durDed}");
        CheckGameOver();
    }

    public void PenalizeDeath()
    {
        totalFoodValue /= 2f;
        totalWaterValue /= 2f;
        totalDurabilityValue /= 2f;
        Debug.Log("<color=red>Player died/timed out! Stash value halved!</color>");
        CheckGameOver();
    }

    private void CheckGameOver()
    {
        if (totalFoodValue <= 0) Debug.Log("[Game Over] Food value depleted!");
        if (totalWaterValue <= 0) Debug.Log("[Game Over] Water value depleted!");
        if (totalDurabilityValue <= 0) Debug.Log("[Game Over] Shelter durability reached zero, shelter breached!");
    }
}