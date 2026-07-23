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
        // 【核心修复】：强行自动双向绑定，再也不怕你在编辑器里漏拖槽位了！
        if (inventory != null) inventory.uiManager = this;
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
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
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