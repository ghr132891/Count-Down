using UnityEngine;

// 路径: Assets/Scripts/UI/InventoryGridUI.cs
public class InventoryGridUI : MonoBehaviour
{
    public PlayerInventory inventory;

    [Header("UI References")]
    public Transform backgroundContainer;
    public Transform itemContainer;

    [Header("Prefabs")]
    public GameObject emptyCellPrefab;
    public GameObject itemUIPrefab;

    [Header("Grid Layout")]
    public int cellSize = 50;
    public int cellSpacing = 2;

    private void Start()
    {
        Vector2 exactGridSize = new Vector2(inventory.columns * cellSize, inventory.rows * cellSize);

        SetupContainer(backgroundContainer.GetComponent<RectTransform>(), exactGridSize);
        SetupContainer(itemContainer.GetComponent<RectTransform>(), exactGridSize);

        GenerateBackgroundGrid();
        RefreshUI();
    }

    private void SetupContainer(RectTransform rect, Vector2 size)
    {
        if (rect == null) return;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(-size.x / 2f, size.y / 2f);
    }

    private void GenerateBackgroundGrid()
    {
        for (int y = 0; y < inventory.rows; y++)
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
    }

    public void RefreshUI()
    {
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var pItem in inventory.placedItems)
        {
            GameObject obj = Instantiate(itemUIPrefab, itemContainer);
            InventoryItemUI itemUI = obj.GetComponent<InventoryItemUI>();
            itemUI.Initialize(this, pItem.instance, pItem.x, pItem.y);
        }
    }

    public Vector2Int GetGridPosition(Vector2 screenPosition)
    {
        RectTransform bgRect = backgroundContainer.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(bgRect, screenPosition, null, out Vector2 localPoint);

        // 【核心优化】：从 FloorToInt 改为 RoundToInt
        // 这一改动直接赋予了 UI 强大的“边缘磁吸”手感，容错率极高
        int x = Mathf.RoundToInt(localPoint.x / cellSize);
        int y = Mathf.RoundToInt(-localPoint.y / cellSize);

        return new Vector2Int(x, y);
    }
}