using System.Collections.Generic;
using UnityEngine;


//物品品实例类 (ItemInstance)
public class ItemInstance
{
    public ItemData data;
    public bool isRotated;

    public ItemInstance(ItemData data, bool isRotated = false)
    {
        this.data = data;
        this.isRotated = isRotated;
    }

    // 动态获取当前的长宽。如果被旋转了，长宽互换
    public int Width => isRotated ? data.height : data.width;
    public int Height => isRotated ? data.width : data.height;
}

// ==========================================
// 2. 玩家背包核心数据逻辑
// ==========================================
public class PlayerInventory : MonoBehaviour
{
    [Header("Grid Inventory Settings")]
    public int columns = 8;
    public int rows = 5;

    public ItemInstance[,] grid;

    public class PlacedItem
    {
        public ItemInstance instance;
        public int x;
        public int y;
        public bool isRotated;
    }

    public List<PlacedItem> placedItems = new List<PlacedItem>();

    public InventoryGridUI uiManager;

    private void Awake()
    {
        grid = new ItemInstance[columns, rows];
    }

    public bool AutoAddItem(ItemInstance itemToAdd)
    {
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                if (CanPlaceItem(itemToAdd, x, y))
                {
                    PlaceItem(itemToAdd, x, y);
                    if (uiManager != null) uiManager.RefreshUI();
                    return true;
                }
            }
        }
        return false;
    }

    public bool TryMoveItem(ItemInstance item, int newX, int newY)
    {
        PlacedItem pItem = placedItems.Find(p => p.instance == item);
        if (pItem == null) return false;

        int oldX = pItem.x;
        int oldY = pItem.y;

        // 记录原来的旋转状态和空中乱转的新状态
        bool oldRotation = pItem.isRotated;
        bool newRotation = item.isRotated;

        // 1. 必须使用原本的状态，才能精准抹除旧网格
        item.isRotated = oldRotation;
        RemoveItemFromGrid(item, oldX, oldY);
        placedItems.Remove(pItem);

        // 2. 换成新的状态，去测试新位置
        item.isRotated = newRotation;

        if (CanPlaceItem(item, newX, newY))
        {
            // 放得下，确认放置
            PlaceItem(item, newX, newY);
            if (uiManager != null) uiManager.RefreshUI();
            return true;
        }
        else
        {
            // 【核心修复】：放不下时，必须再次变回原先的旋转状态，才能原样塞回去！
            item.isRotated = oldRotation;
            PlaceItem(item, oldX, oldY);
            return false;
        }
    }

    private void RemoveItemFromGrid(ItemInstance item, int startX, int startY)
    {
        for (int y = startY; y < startY + item.Height; y++)
        {
            for (int x = startX; x < startX + item.Width; x++)
            {
                grid[x, y] = null;
            }
        }
    }

    public bool CanPlaceItem(ItemInstance item, int startX, int startY)
    {
        if (startX < 0 || startY < 0 || startX + item.Width > columns || startY + item.Height > rows)
            return false;

        for (int y = startY; y < startY + item.Height; y++)
        {
            for (int x = startX; x < startX + item.Width; x++)
            {
                if (grid[x, y] != null) return false;
            }
        }
        return true;
    }

    private void PlaceItem(ItemInstance item, int startX, int startY)
    {
        for (int y = startY; y < startY + item.Height; y++)
        {
            for (int x = startX; x < startX + item.Width; x++)
            {
                grid[x, y] = item;
            }
        }
        placedItems.Add(new PlacedItem { instance = item, x = startX, y = startY, isRotated = item.isRotated });
    }
}