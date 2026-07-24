using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 路径: Assets/Scripts/UI/DiaryUI.cs
public class DiaryUI : MonoBehaviour
{
    [Header("Main UI")]
    public CanvasGroup uiCanvasGroup;
    public GameObject pageStats;
    public GameObject pageStory;

    [Header("Page Navigation")]
    public Button nextPageBtn;
    public Button prevPageBtn;

    [Header("Stats References")]
    public PlayerController player;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI foodText;
    public TextMeshProUGUI waterText;
    public TextMeshProUGUI durabilityText;
    public TextMeshProUGUI injuryText;

    [Header("Story References")]
    public TextMeshProUGUI storyDescription;
    public Button option1Btn;
    public TextMeshProUGUI option1Text;
    public Button option2Btn;
    public TextMeshProUGUI option2Text;

    // 【新增】当前正在展示的剧情数据
    private StoryEventData currentEvent;

    private void Start()
    {
        SetPanelActive(false);

        // 绑定按钮
        if (option1Btn != null) option1Btn.onClick.AddListener(() => OnOptionSelected(0));
        if (option2Btn != null) option2Btn.onClick.AddListener(() => OnOptionSelected(1));
        if (nextPageBtn != null) nextPageBtn.onClick.AddListener(ShowStoryPage);
        if (prevPageBtn != null) prevPageBtn.onClick.AddListener(ShowStatsPage);
    }

    public void ToggleDiary()
    {
        bool isOpening = uiCanvasGroup.alpha == 0;
        SetPanelActive(isOpening);

        if (isOpening)
        {
            ShowStatsPage();
        }
    }

    public void ShowStatsPage()
    {
        pageStats.SetActive(true);
        pageStory.SetActive(false);

        if (player != null)
            hpText.text = $"生命 (HP): {Mathf.RoundToInt(player.currentHealth)} / {player.maxHealth}";

        if (SurvivalManager.Instance != null)
        {
            foodText.text = $"食物: {SurvivalManager.Instance.food} (-3/天)";
            waterText.text = $"水: {SurvivalManager.Instance.water} (-2/天)";
            durabilityText.text = $"小屋耐久: {SurvivalManager.Instance.shelterDurability} / 100";
            injuryText.text = $"受伤计数: {SurvivalManager.Instance.injuryCount}";
        }
    }

    // 【核心重构】动态加载剧情数据
    public void LoadEventAndShow(StoryEventData eventData)
    {
        currentEvent = eventData;
        ShowStoryPage();
    }

    public void ShowStoryPage()
    {
        pageStats.SetActive(false);
        pageStory.SetActive(true);

        // 如果还没有分发剧情，显示默认占位符
        if (currentEvent == null)
        {
            storyDescription.text = $"第 {GameManager.Instance.currentDay} 天\n\n今天十分平静，什么事情也没有发生。";
            option1Btn.gameObject.SetActive(false);
            option2Btn.gameObject.SetActive(false);
            return;
        }

        // 渲染剧情文本
        storyDescription.text = $"第 {GameManager.Instance.currentDay} 天\n\n{currentEvent.description}";

        // 渲染选项1
        if (currentEvent.options.Count > 0)
        {
            option1Btn.gameObject.SetActive(true);
            option1Text.text = currentEvent.options[0].optionText;
        }
        else option1Btn.gameObject.SetActive(false);

        // 渲染选项2
        if (currentEvent.options.Count > 1)
        {
            option2Btn.gameObject.SetActive(true);
            option2Text.text = currentEvent.options[1].optionText;
        }
        else option2Btn.gameObject.SetActive(false);
    }

    // 【核心重构】通用选项结算器
    private void OnOptionSelected(int index)
    {
        if (currentEvent == null || index >= currentEvent.options.Count) return;

        StoryOption selectedOption = currentEvent.options[index];

        // 1. 结算生存物资
        if (SurvivalManager.Instance != null)
        {
            SurvivalManager.Instance.injuryCount += selectedOption.injuryChange;
            SurvivalManager.Instance.ModifyStats(selectedOption.foodChange, selectedOption.waterChange, selectedOption.durabilityChange);
        }

        // 2. 结算玩家深度属性 (上限扣减等)
        if (player != null)
        {
            player.ModifyCoreStats(selectedOption.hpChange, selectedOption.maxHpChange, selectedOption.maxStaminaChange);
        }

        // 清空当前事件，防止玩家反复点击刷数值
        currentEvent = null;

        // 强制切回状态页看结果
        ShowStatsPage();
    }

    public bool IsOpen() { return uiCanvasGroup != null && uiCanvasGroup.alpha > 0; }

    private void SetPanelActive(bool active)
    {
        uiCanvasGroup.alpha = active ? 1f : 0f;
        uiCanvasGroup.interactable = active;
        uiCanvasGroup.blocksRaycasts = active;
    }
}