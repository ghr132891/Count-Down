using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 路径: Assets/Scripts/Item/ItemDatabase.cs
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }
    public List<ItemData> allItems = new List<ItemData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadDatabase()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("ItemsDB");
        if (jsonFile != null)
        {
            ItemDataList loadedData = JsonUtility.FromJson<ItemDataList>(jsonFile.text);
            allItems = loadedData.items;

            foreach (var item in allItems)
            {
                if (item.quality == "Common") item.itemColor = Color.gray;
                else if (item.quality == "Rare") item.itemColor = new Color(0.2f, 0.6f, 1f);
                else if (item.quality == "Legendary") item.itemColor = new Color(1f, 0.8f, 0f);

                item.iconSprite = Resources.Load<Sprite>($"ItemIcons/{item.itemName}");
                if (item.iconSprite == null)
                {
                    Debug.LogWarning($"[Icon Missing] 找不到图标: {item.itemName}");
                }
            }
            Debug.Log($"Successfully loaded JSON item database, containing {allItems.Count} items!");
        }
        else
        {
            Debug.LogError("Could not find ItemsDB.json! Please ensure it is placed in the Assets/Resources directory.");
        }
    }

    // 【核心修改】加入 isPrecious 参数，判断是否来自珍贵宝箱
    public ItemData GetRandomLoot(bool isPrecious = false)
    {
        string targetQuality = GetRandomQuality(isPrecious);
        Vector2Int targetSize = GetRandomSize();

        List<ItemData> matches = allItems.Where(i => i.quality == targetQuality && i.width == targetSize.x && i.height == targetSize.y).ToList();

        if (matches.Count > 0)
        {
            ItemData original = matches[Random.Range(0, matches.Count)];
            return CloneItem(original);
        }
        else
        {
            ItemData fallback = allItems[Random.Range(0, allItems.Count)];
            return CloneItem(fallback);
        }
    }

    // 【核心修改】双轨制爆率算法
    private string GetRandomQuality(bool isPrecious)
    {
        float roll = Random.Range(0f, 100f);

        if (isPrecious)
        {
            // 珍贵宝箱的爆率 (Legendary 概率大幅提升到 30%!)
            if (roll < 20f) return "Common";    // 20% 普通
            if (roll < 70f) return "Rare";      // 50% 稀有
            return "Legendary";                 // 30% 传说
        }
        else
        {
            // 普通宝箱的爆率
            if (roll < 60f) return "Common";    // 60% 普通
            if (roll < 92f) return "Rare";      // 32% 稀有
            return "Legendary";                 // 8% 传说
        }
    }

    private Vector2Int GetRandomSize()
    {
        float roll = Random.Range(0f, 100f);
        if (roll < 24f) return new Vector2Int(1, 1);
        if (roll < 40f) return new Vector2Int(1, 2);
        if (roll < 52f) return new Vector2Int(1, 3);
        if (roll < 64f) return new Vector2Int(2, 2);
        if (roll < 76f) return new Vector2Int(2, 3);
        if (roll < 86f) return new Vector2Int(2, 4);
        if (roll < 94f) return new Vector2Int(3, 3);
        return new Vector2Int(3, 4);
    }

    private ItemData CloneItem(ItemData source)
    {
        return new ItemData
        {
            itemName = source.itemName,
            quality = source.quality,
            category = source.category,
            width = source.width,
            height = source.height,
            foodValue = source.foodValue,
            waterValue = source.waterValue,
            durabilityValue = source.durabilityValue,
            itemColor = source.itemColor,
            iconSprite = source.iconSprite
        };
    }
}