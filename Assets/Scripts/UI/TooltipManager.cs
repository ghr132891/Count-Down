using UnityEngine;
using TMPro;
using UnityEngine.UI;

// 路径: Assets/Scripts/UI/TooltipManager.cs
public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    public TextMeshProUGUI tooltipText;
    private RectTransform rectTransform;

    private void Awake()
    {
        Instance = this;
        rectTransform = GetComponent<RectTransform>();
        // 游戏开始时默认隐藏
        gameObject.SetActive(false);
    }

    private void Update()
    {
        // 让提示框紧跟鼠标，并向右下角偏移一点，防止被鼠标挡住
        transform.position = Input.mousePosition + new Vector3(15f, -15f, 0f);
    }

    public void ShowTooltip(string content)
    {
        tooltipText.text = content;
        gameObject.SetActive(true);
        // 强制刷新 UI 布局，让背景图瞬间包裹住文字
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        // 确保提示框永远在最顶层显示
        transform.SetAsLastSibling();
    }

    public void HideTooltip()
    {
        gameObject.SetActive(false);
    }
}   