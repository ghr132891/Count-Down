using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Path: Assets/Scripts/UI/InventoryItemUI.cs
// 【核心修改】继承 IPointerClickHandler 以接收鼠标点击事件
public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    // 全局静态变量，用于记录当前唯一被点击查看的物品
    public static InventoryItemUI SelectedItem;
    private static Texture2D tooltipBgTexture;

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
    public float maxDropDistance = 3f; // 允许玩家丢弃物品的最大范围

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

    // 【新增】处理鼠标左键查看信息和右键丢弃
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            DropItemToWorld();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (SelectedItem == this) SelectedItem = null;
            else SelectedItem = this;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        SelectedItem = null; // 拖拽时自动关闭信息面板，防止遮挡视野

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

        // 【核心修改】如果拖到了没有任何 UI 网格的地方，执行拖拽丢弃
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

    // 【新增】安全丢弃物品到真实世界的逻辑
    private void DropItemToWorld()
    {
        if (GameManager.Instance == null || GameManager.Instance.survivalLootPrefabs.Length == 0) return;

        // 1. 获取鼠标所在的世界坐标
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        // 2. 限制丢弃距离，防止物品被扔到穿墙或者超远距离
        Vector3 playerPos = GameManager.Instance.player.transform.position;
        Vector3 direction = mouseWorldPos - playerPos;
        Vector3 dropPos;

        if (direction.magnitude > maxDropDistance)
        {
            dropPos = playerPos + direction.normalized * maxDropDistance; // 限制在最大半径内
        }
        else
        {
            dropPos = mouseWorldPos;
        }

        // 3. 生成模型并转移数据
        GameObject prefab = GameManager.Instance.survivalLootPrefabs[0];
        GameObject lootObj = Instantiate(prefab, dropPos, Quaternion.identity);

        InteractableLoot lootScript = lootObj.GetComponent<InteractableLoot>();
        if (lootScript != null)
        {
            lootScript.SetupDroppedItem(itemInstance);
        }

        // 4. 从背包/仓库彻底移除
        if (SelectedItem == this) SelectedItem = null;
        gridUI.inventory.RemoveItem(itemInstance);
        gridUI.RefreshUI();
    }

    // 【新增】使用 OnGUI 绘制极简且在最顶层的信息浮窗，完全无视 UI 层级遮挡问题
    private void OnGUI()
    {
        if (SelectedItem == this && itemInstance != null)
        {
            Vector3 screenPos = Input.mousePosition;
            float guiY = Screen.height - screenPos.y;

            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = 16;
            style.richText = true;

            // 缓存背景贴图，避免每帧重复创建引发内存泄漏
            if (tooltipBgTexture == null)
            {
                tooltipBgTexture = new Texture2D(1, 1);
                tooltipBgTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.85f));
                tooltipBgTexture.Apply();
            }
            style.normal.background = tooltipBgTexture;
            style.normal.textColor = Color.white;

            string info = $"<b><color=#{ColorUtility.ToHtmlStringRGB(itemInstance.data.itemColor)}>{itemInstance.data.quality} {itemInstance.data.itemName}</color></b>\n" +
                          $"Category: {itemInstance.data.category}\n" +
                          $"Size: {itemInstance.Width}x{itemInstance.Height}\n\n" +
                          $"<color=#ffaa00>Food: {itemInstance.data.foodValue}</color>\n" +
                          $"<color=#00ccff>Water: {itemInstance.data.waterValue}</color>\n" +
                          $"<color=#aaffaa>Durability: {itemInstance.data.durabilityValue}</color>\n\n" +
                          $"<size=12><i>Right-Click: Drop Item</i></size>";

            // 动态计算文字宽高
            GUIContent content = new GUIContent(info);
            Vector2 size = style.CalcSize(content);

            // 确保面板紧随鼠标且不会跑出屏幕之外
            float x = screenPos.x + 15f;
            float y = guiY + 15f;
            if (x + size.x > Screen.width) x = Screen.width - size.x - 10f;
            if (y + size.y > Screen.height) y = Screen.height - size.y - 10f;

            GUI.Box(new Rect(x, y, size.x + 20, size.y + 20), info, style);
        }
    }
}