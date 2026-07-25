using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Path: Assets/Scripts/UI/DailySummaryUI.cs
public class DailySummaryUI : MonoBehaviour
{
    public enum SummaryState { Morning, Evening }
    private SummaryState currentState;

    [Header("UI References")]
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI resultText;
    public Button nextDayBtn;

    private void Start()
    {
        panel.SetActive(false);
        nextDayBtn.onClick.AddListener(OnBtnClicked);
    }

    // 早上面板：接收并显示三个扣除数值
    public void ShowMorningPanel(int day, float foodDed, float waterDed, float durDed)
    {
        currentState = SummaryState.Morning;
        panel.SetActive(true);

        titleText.text = $"Day {day} Begins";
        if (day == 1)
        {
            resultText.text = "Day 1, no survival deduction.\nYou can go out and explore when you are ready. The countdown will start the moment you leave the shelter!";
        }
        else
        {
            resultText.text = $"Last night's survival cost:\nFood Value: <color=red>-{foodDed}</color>\nWater Value: <color=red>-{waterDed}</color>\nShelter Durability: <color=red>-{durDed}</color>\n\n" +
                              $"Current Total Food: {SurvivalManager.Instance.totalFoodValue}\n" +
                              $"Current Total Water: {SurvivalManager.Instance.totalWaterValue}\n" +
                              $"Current Total Durability: {SurvivalManager.Instance.totalDurabilityValue}";
        }
        nextDayBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Start Today";
    }

    // 晚上面板：累加三个数值
    public void ShowEveningPanel(PlayerInventory stash)
    {
        currentState = SummaryState.Evening;
        panel.SetActive(true);
        float addedFood = 0f;
        float addedWater = 0f;
        float addedDur = 0f;

        if (stash != null)
        {
            foreach (var item in stash.placedItems)
            {
                addedFood += item.instance.data.foodValue;
                addedWater += item.instance.data.waterValue;
                addedDur += item.instance.data.durabilityValue;
            }
            stash.ClearInventory();
        }

        if (SurvivalManager.Instance != null)
        {
            SurvivalManager.Instance.AddValues(addedFood, addedWater, addedDur);

            titleText.text = $"Day {GameManager.Instance.currentDay} Summary Report";
            resultText.text = $"Today's stash conversion:\nFood Value: <color=green>+{addedFood}</color>\nWater Value: <color=green>+{addedWater}</color>\nShelter Durability: <color=green>+{addedDur}</color>\n\n" +
                              $"Current Total Food: {SurvivalManager.Instance.totalFoodValue}\n" +
                              $"Current Total Water: {SurvivalManager.Instance.totalWaterValue}\n" +
                              $"Current Total Durability: {SurvivalManager.Instance.totalDurabilityValue}";
        }
        nextDayBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Proceed to Next Day";
    }

    private void OnBtnClicked()
    {
        panel.SetActive(false);

        if (currentState == SummaryState.Evening)
        {
            if (GameManager.Instance != null) GameManager.Instance.AdvanceToNextDay();
        }
    }
}