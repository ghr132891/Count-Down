using UnityEngine;
using System.Collections.Generic;

// 路径: Assets/Scripts/UI/InventoryGridUI.cs
public class InventoryGridUI : MonoBehaviour
{
    public static List<InventoryGridUI> AllGrids = new List<InventoryGridUI>();
    public PlayerInventory inventory;

    [Header("UI References")]
    public CanvasGroup uiCanvasGroup;
    public bool isStash = false;

    [Header("Background Settings (Padding)")]
    public RectTransform panelBackground; // 【新增】独立的背景图层
    public int paddingLeft = 30;          // 【新增】左侧留白距离
    public int paddingRight = 30;         // 【新增】右侧留白距离
    public int paddingTop = 40;           // 【新增】顶部留白距离
    public int paddingBottom = 40;        // 【新增】底部留白距离

    public Transform backgroundContainer;
    public Transform itemContainer;

    [Header("Prefabs")]
    public GameObject emptyCellPrefab;
    public GameObject itemUIPrefab;

    [Header("Grid Layout")]
    public int cellSize = 50;
    public int cellSpacing = 2;

    private void Awake()
    {
        AllGrids.Add(this);
    }

    private void OnDestroy()
    {
        AllGrids.Remove(this);
    }

    private void Start()
    {
        if (itemContainer != null && backgroundContainer != null)
        {
            itemContainer.SetParent(backgroundContainer, false);
        }

        Vector2 exactGridSize = new Vector2(inventory.columns * cellSize, inventory.rows * cellSize);
        SetupInnerContainer(backgroundContainer.GetComponent<RectTransform>(), exactGridSize);
        SetupInnerContainer(itemContainer.GetComponent<RectTransform>(), exactGridSize);

        // --- 【核心修改】应用背景图和边距 ---
        SetupBackground(exactGridSize);

        GenerateBackgroundGrid(0, inventory.rows);
        RefreshUI();
        SetPanelActive(false);
    }

    private void Update()
    {
        if (!isStash && (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Tab)))
        {
            TogglePanel();
        }
    }

    public void SetupInnerContainer(RectTransform rect, Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(-size.x / 2f, size.y / 2f);
    }

    // --- 【新增方法】动态计算羊皮纸的大小和偏移 ---
    private void SetupBackground(Vector2 gridSize)
    {
        if (panelBackground == null) return;

        panelBackground.anchorMin = new Vector2(0.5f, 0.5f);
        panelBackground.anchorMax = new Vector2(0.5f, 0.5f);
        panelBackground.pivot = new Vector2(0, 1);

        // 加上四周边距，撑大羊皮纸
        panelBackground.sizeDelta = new Vector2(gridSize.x + paddingLeft + paddingRight, gridSize.y + paddingTop + paddingBottom);

        // 向左上方偏移，把格子包裹在中心
        panelBackground.anchoredPosition = new Vector2(-gridSize.x / 2f - paddingLeft, gridSize.y / 2f + paddingTop);

        // 强制渲染在最底层，防止挡住格子
        panelBackground.SetAsFirstSibling();
    }

    private void GenerateBackgroundGrid(int startRow, int endRow)
    {
        for (int y = startRow; y < endRow; y++)
        {
            for (int x = 0; x < inventory.columns; x++)
            {
                GameObject cell = Instantiate(emptyCellPrefab, backgroundContainer);
                RectTransform rect = cell.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(cellSize - cellSpacing, cellSize - cellSpacing);
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
            }
        }
        if (itemContainer != null) itemContainer.SetAsLastSibling();
    }

    public void ExpandUI(int oldRows, int newRows)
    {
        Vector2 exactGridSize = new Vector2(inventory.columns * cellSize, newRows * cellSize);
        SetupInnerContainer(backgroundContainer.GetComponent<RectTransform>(), exactGridSize);
        SetupInnerContainer(itemContainer.GetComponent<RectTransform>(), exactGridSize);

        // 扩展背包时，同步放大羊皮纸
        SetupBackground(exactGridSize);

        GenerateBackgroundGrid(oldRows, newRows);
    }

    public void RefreshUI()
    {
        foreach (Transform child in itemContainer) Destroy(child.gameObject);

        foreach (var pItem in inventory.placedItems)
        {
            GameObject obj = Instantiate(itemUIPrefab, itemContainer);
            InventoryItemUI itemUI = obj.GetComponent<InventoryItemUI>();
            itemUI.Initialize(this, pItem.instance, pItem.x, pItem.y);
        }
    }

    public void TogglePanel()
    {
        if (uiCanvasGroup == null) return;
        bool isOpening = uiCanvasGroup.alpha == 0;
        SetPanelActive(isOpening);
    }

    public bool IsOpen()
    {
        return uiCanvasGroup != null && uiCanvasGroup.alpha > 0;
    }

    public void SetPanelActive(bool active)
    {
        if (uiCanvasGroup == null) return;
        uiCanvasGroup.alpha = active ? 1f : 0f;
        uiCanvasGroup.interactable = active;
        uiCanvasGroup.blocksRaycasts = active;
    }
}