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

    private Transform originalParent;
    private bool wasRotated;
    private bool isDragging = false;

    private Canvas canvas;
    private RectTransform canvasRect;
    private Vector3 dragOffset;
    private Vector2 gridGrabOffset;

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
            rectTransform.sizeDelta = new Vector2(
                itemInstance.Width * gridUI.cellSize - gridUI.cellSpacing,
                itemInstance.Height * gridUI.cellSize - gridUI.cellSpacing
            );
        }
    }

    private void UpdateSizeAndPosition(int x, int y)
    {
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);

        rectTransform.sizeDelta = new Vector2(
            itemInstance.Width * gridUI.cellSize - gridUI.cellSpacing,
            itemInstance.Height * gridUI.cellSize - gridUI.cellSpacing
        );
        rectTransform.anchoredPosition = new Vector2(x * gridUI.cellSize, -y * gridUI.cellSize);

        Vector3 pos = rectTransform.localPosition;
        pos.z = 0;
        rectTransform.localPosition = pos;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        wasRotated = itemInstance.isRotated;
        isDragging = true;

        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPos);
        gridGrabOffset = new Vector2(localPointerPos.x / gridUI.cellSize, -localPointerPos.y / gridUI.cellSize);

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
        image.raycastTarget = false;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
        {
            dragOffset = transform.position - worldPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvasRect != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
        {
            transform.position = worldPoint + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        image.raycastTarget = true;
        isDragging = false;
        transform.SetParent(originalParent, true);

        InventoryGridUI targetGrid = null;
        foreach (var grid in InventoryGridUI.AllGrids)
        {
            if (grid.IsOpen())
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(grid.backgroundContainer.GetComponent<RectTransform>(), Input.mousePosition, eventData.pressEventCamera))
                {
                    targetGrid = grid;
                    break;
                }
            }
        }

        bool moveSuccess = false;

        if (targetGrid != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetGrid.backgroundContainer.GetComponent<RectTransform>(), Input.mousePosition, eventData.pressEventCamera, out Vector2 localMousePos);

            float exactX = (localMousePos.x / targetGrid.cellSize) - gridGrabOffset.x;
            float exactY = (-localMousePos.y / targetGrid.cellSize) - gridGrabOffset.y;

            int gridX = Mathf.RoundToInt(exactX);
            int gridY = Mathf.RoundToInt(exactY);

            if (targetGrid == this.gridUI)
            {
                moveSuccess = gridUI.inventory.TryMoveItem(itemInstance, gridX, gridY);
                // 【核心修复】：同仓移动无论成败，强制执行吸附刷新，绝不允许悬停！
                if (moveSuccess) gridUI.RefreshUI();
            }
            else
            {
                if (targetGrid.inventory.CanPlaceItem(itemInstance, gridX, gridY))
                {
                    gridUI.inventory.RemoveItem(itemInstance);
                    targetGrid.inventory.PlaceItem(itemInstance, gridX, gridY);

                    gridUI.RefreshUI();
                    targetGrid.RefreshUI();
                    moveSuccess = true;
                }
            }
        }

        // 放置失败或者扔到空白处，强制退回原位吸附
        if (!moveSuccess)
        {
            itemInstance.isRotated = wasRotated;
            gridUI.RefreshUI();
        }
    }
}