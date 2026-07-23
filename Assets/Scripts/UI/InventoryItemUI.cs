using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 路径: Assets/Scripts/UI/InventoryItemUI.cs
public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private InventoryGridUI gridUI;
    private ItemInstance itemInstance;
    private RectTransform rectTransform;
    private Image image;

    private Vector2 originalPosition;
    private bool wasRotated;
    private bool isDragging = false;
    private Vector2 dragOffset;

    public void Initialize(InventoryGridUI grid, ItemInstance item, int x, int y)
    {
        gridUI = grid;
        itemInstance = item;
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        image.color = item.data.itemColor;
        UpdateSizeAndPosition(x, y);
    }

    private void Update()
    {
        if (isDragging && Input.GetKeyDown(KeyCode.R))
        {
            itemInstance.isRotated = !itemInstance.isRotated;

            // 【修改】旋转时更新尺寸，减去间隙
            rectTransform.sizeDelta = new Vector2(
                itemInstance.Width * gridUI.cellSize - gridUI.cellSpacing,
                itemInstance.Height * gridUI.cellSize - gridUI.cellSpacing
            );
        }
    }

    private void UpdateSizeAndPosition(int x, int y)
    {
        // 【修改】初始化尺寸，减去间隙，让物品能完美卡在带有缝隙的背景网格中
        rectTransform.sizeDelta = new Vector2(
            itemInstance.Width * gridUI.cellSize - gridUI.cellSpacing,
            itemInstance.Height * gridUI.cellSize - gridUI.cellSpacing
        );

        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);

        // 坐标位置绝对不变
        rectTransform.anchoredPosition = new Vector2(x * gridUI.cellSize, -y * gridUI.cellSize);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.localPosition;
        wasRotated = itemInstance.isRotated;
        isDragging = true;

        transform.SetAsLastSibling();
        image.raycastTarget = false;

        dragOffset = (Vector2)rectTransform.position - eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position + dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        image.raycastTarget = true;
        isDragging = false;

        Vector2Int gridPos = gridUI.GetGridPosition(rectTransform.position);
        bool moveSuccess = gridUI.inventory.TryMoveItem(itemInstance, gridPos.x, gridPos.y);

        if (!moveSuccess)
        {
            itemInstance.isRotated = wasRotated;
            gridUI.RefreshUI();
        }
    }
}