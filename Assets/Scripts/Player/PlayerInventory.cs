using System.Collections.Generic;
using UnityEngine;

// 路径: Assets/Scripts/Player/PlayerInventory.cs
public class ItemInstance
{
    public ItemData data;
    public bool isRotated;
    public ItemInstance(ItemData data, bool isRotated = false)
    {
        this.data = data;
        this.isRotated = isRotated;
    }
    public int Width => isRotated ? data.height : data.width;
    public int Height => isRotated ? data.width : data.height;
}

public class PlayerInventory : MonoBehaviour
{
    [Header("Grid Inventory Settings")]
    public int columns = 8;
    public int rows = 5;

    public bool isInfiniteCapacity = false;

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

        if (isInfiniteCapacity)
        {
            ExpandInventory(rows + 5);
            return AutoAddItem(itemToAdd);
        }

        return false;
    }

    public bool TryMoveItem(ItemInstance item, int newX, int newY)
    {
        PlacedItem pItem = placedItems.Find(p => p.instance == item);
        if (pItem == null) return false;
        int oldX = pItem.x;
        int oldY = pItem.y;

        bool oldRotation = pItem.isRotated;
        bool newRotation = item.isRotated;

        item.isRotated = oldRotation;
        RemoveItemFromGrid(item, oldX, oldY);
        placedItems.Remove(pItem);

        item.isRotated = newRotation;
        if (CanPlaceItem(item, newX, newY))
        {
            PlaceItem(item, newX, newY);
            if (uiManager != null) uiManager.RefreshUI();
            return true;
        }
        else
        {
            item.isRotated = oldRotation;
            PlaceItem(item, oldX, oldY);
            return false;
        }
    }

    public void RemoveItem(ItemInstance item)
    {
        PlacedItem pItem = placedItems.Find(p => p.instance == item);
        if (pItem != null)
        {
            item.isRotated = pItem.isRotated;
            RemoveItemFromGrid(item, pItem.x, pItem.y);
            placedItems.Remove(pItem);
        }
    }

    private void RemoveItemFromGrid(ItemInstance item, int startX, int startY)
    {
        for (int y = startY; y < startY + item.Height; y++)
        {
            for (int x = startX; x < startX + item.Width; x++)
            {
                if (x < columns && y < rows) grid[x, y] = null;
            }
        }
    }

    public bool CanPlaceItem(ItemInstance item, int startX, int startY)
    {
        if (startX < 0 || startY < 0 || startX + item.Width > columns) return false;

        // 【核心修复】：如果是无限仓库，允许你把物品强行拖到虚空区域！
        if (!isInfiniteCapacity && startY + item.Height > rows) return false;

        // 只检测当前已有网格内的碰撞
        int checkHeight = Mathf.Min(startY + item.Height, rows);
        for (int y = startY; y < checkHeight; y++)
        {
            for (int x = startX; x < startX + item.Width; x++)
            {
                if (grid[x, y] != null) return false;
            }
        }
        return true;
    }

    public void PlaceItem(ItemInstance item, int startX, int startY)
    {
        // 【核心修复】：如果存放的Y轴超出了现有行数，立刻向下扩容生长！
        if (isInfiniteCapacity && startY + item.Height > rows)
        {
            ExpandInventory(startY + item.Height + 4);
        }

        for (int y = startY; y < startY + item.Height; y++)
        {
            for (int x = startX; x < startX + item.Width; x++)
            {
                grid[x, y] = item;
            }
        }
        placedItems.Add(new PlacedItem { instance = item, x = startX, y = startY, isRotated = item.isRotated });
    }

    private void ExpandInventory(int newRows)
    {
        ItemInstance[,] newGrid = new ItemInstance[columns, newRows];
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                newGrid[x, y] = grid[x, y];
            }
        }

        int oldRows = rows;
        rows = newRows;
        grid = newGrid;

        if (uiManager != null) uiManager.ExpandUI(oldRows, newRows);
    }
}