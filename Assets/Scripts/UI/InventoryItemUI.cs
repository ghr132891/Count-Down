using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Path: Assets/Scripts/UI/InventoryItemUI.cs
// 【核心修改】引入了 IPointerEnterHandler (鼠标移入) 和 IPointerExitHandler (鼠标移出)
public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private InventoryGridUI gridUI;
    private ItemInstance itemInstance;
    private RectTransform rectTransform;

    private Image rootImage;
    private Image visualImage;
    private RectTransform visualRect;

    private Transform originalParent;
    private bool wasRotated;
    private bool isDragging = false;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Vector3 dragOffset;
    private Vector2 gridGrabOffset;

    [Header("Drop Settings")]
    public float maxDropDistance = 3f;

    public void Initialize(InventoryGridUI grid, ItemInstance item, int x, int y)
    {
        gridUI = grid;
        itemInstance = item;
        rectTransform = GetComponent<RectTransform>();

        rootImage = GetComponent<Image>();
        rootImage.color = new Color(0f, 0f, 0f, 0.4f);

        if (visualImage == null)
        {
            GameObject vObj = new GameObject("VisualImage");
            visualRect = vObj.AddComponent<RectTransform>();
            visualRect.SetParent(this.transform, false);

            visualRect.anchorMin = new Vector2(0.5f, 0.5f);
            visualRect.anchorMax = new Vector2(0.5f, 0.5f);
            visualRect.pivot = new Vector2(0.5f, 0.5f);
            visualRect.anchoredPosition = Vector2.zero;

            visualImage = vObj.AddComponent<Image>();
            visualImage.raycastTarget = false;
            visualImage.preserveAspect = true;
        }

        if (item.data.iconSprite != null)
        {
            visualImage.sprite = item.data.iconSprite;
            visualImage.color = Color.white;
        }
        else
        {
            visualImage.sprite = null;
            visualImage.color = item.data.itemColor;
        }

        UpdateSizeAndPosition(x, y);
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

        visualRect.sizeDelta = new Vector2(
            itemInstance.data.width * gridUI.cellSize - gridUI.cellSpacing,
            itemInstance.data.height * gridUI.cellSize - gridUI.cellSpacing
        );

        visualRect.localEulerAngles = itemInstance.isRotated ? new Vector3(0, 0, 90) : Vector3.zero;
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

            visualRect.localEulerAngles = itemInstance.isRotated ? new Vector3(0, 0, 90) : Vector3.zero;
        }
    }

    // --- 【现代悬浮提示逻辑】 ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging) return;

        // 这里把颜色改成了更深的红、蓝、绿，防止在羊皮纸上看不清
        string info = $"<b><color=#{ColorUtility.ToHtmlStringRGB(itemInstance.data.itemColor)}>{itemInstance.data.quality} {itemInstance.data.itemName}</color></b>\n" +
                      $"Category: {itemInstance.data.category}\n" +
                      $"Size: {itemInstance.Width}x{itemInstance.Height}\n\n" +
                      $"<color=#8B0000>Food: {itemInstance.data.foodValue}</color>\n" +
                      $"<color=#00008B>Water: {itemInstance.data.waterValue}</color>\n" +
                      $"<color=#006400>Durability: {itemInstance.data.durabilityValue}</color>\n\n" +
                      $"<size=12><i>Right-Click: Drop Item</i></size>";

        if (TooltipManager.Instance != null) TooltipManager.Instance.ShowTooltip(info);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null) TooltipManager.Instance.HideTooltip();
    }

    // --- 【右键丢弃逻辑】 ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (TooltipManager.Instance != null) TooltipManager.Instance.HideTooltip();
            DropItemToWorld();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null) TooltipManager.Instance.HideTooltip(); // 拖拽时隐藏提示框

        originalParent = transform.parent;
        wasRotated = itemInstance.isRotated;
        isDragging = true;
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPos);
        gridGrabOffset = new Vector2(localPointerPos.x / gridUI.cellSize, -localPointerPos.y / gridUI.cellSize);

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

        rootImage.raycastTarget = false;

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
        rootImage.raycastTarget = true;
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

        if (targetGrid == null)
        {
            DropItemToWorld();
            return;
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

        if (!moveSuccess)
        {
            itemInstance.isRotated = wasRotated;
            gridUI.RefreshUI();
        }
    }

    private void DropItemToWorld()
    {
        if (GameManager.Instance == null || GameManager.Instance.survivalLootPrefabs.Length == 0) return;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector3 playerPos = GameManager.Instance.player.transform.position;
        Vector3 direction = mouseWorldPos - playerPos;
        Vector3 dropPos;

        if (direction.magnitude > maxDropDistance)
        {
            dropPos = playerPos + direction.normalized * maxDropDistance;
        }
        else
        {
            dropPos = mouseWorldPos;
        }

        GameObject prefab = GameManager.Instance.survivalLootPrefabs[0];
        GameObject lootObj = Instantiate(prefab, dropPos, Quaternion.identity);

        InteractableLoot lootScript = lootObj.GetComponent<InteractableLoot>();
        if (lootScript != null)
        {
            lootScript.SetupDroppedItem(itemInstance);
        }

        gridUI.inventory.RemoveItem(itemInstance);
        gridUI.RefreshUI();
    }
}