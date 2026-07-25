using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Path: Assets/Scripts/Item/ItemDatabase.cs
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

            // 【测试特供】一次性加载 ItemIcons 文件夹下的所有图片作为备用图库
            Sprite[] testSprites = Resources.LoadAll<Sprite>("ItemIcons");

            foreach (var item in allItems)
            {
                if (item.quality == "Common") item.itemColor = Color.gray;
                else if (item.quality == "Rare") item.itemColor = new Color(0.2f, 0.6f, 1f);
                else if (item.quality == "Legendary") item.itemColor = new Color(1f, 0.8f, 0f);

                // 1. 尝试精准匹配（如果以后你补充了对应的同名图片，会优先用对的）
                item.iconSprite = Resources.Load<Sprite>($"ItemIcons/{item.itemName}");

                // 2. 【测试特供】如果没找到同名图片，且备用图库里有图，就随机抽一张给它用
                if (item.iconSprite == null && testSprites.Length > 0)
                {
                    item.iconSprite = testSprites[Random.Range(0, testSprites.Length)];
                }
                // 3. 如果连备用图库都是空的，才会报错
                else if (item.iconSprite == null)
                {
                    Debug.LogWarning($"[Icon Missing] Cannot find any icon for: {item.itemName}");
                }
            }
            Debug.Log($"Successfully loaded JSON item database, containing {allItems.Count} items! Loaded {testSprites.Length} test sprites.");
        }
        else
        {
            Debug.LogError("Could not find ItemsDB.json! Please ensure it is placed in the Assets/Resources directory.");
        }
    }

    public ItemData GetRandomLoot()
    {
        string targetQuality = GetRandomQuality();
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

    private string GetRandomQuality()
    {
        float roll = Random.Range(0f, 100f);
        if (roll < 60f) return "Common";
        if (roll < 92f) return "Rare";
        return "Legendary";
    }

    private Vector2Int GetRandomSize()
    {
        float roll = Random.Range(0f, 100f);
        if (roll < 30f) return new Vector2Int(1, 1);
        if (roll < 48f) return new Vector2Int(1, 2);
        if (roll < 62f) return new Vector2Int(1, 3);
        if (roll < 74f) return new Vector2Int(2, 2);
        if (roll < 84f) return new Vector2Int(2, 3);
        if (roll < 92f) return new Vector2Int(2, 4);
        if (roll < 97f) return new Vector2Int(3, 3);
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
            iconSprite = source.iconSprite // 确保克隆时把抽到的图片也带上
        };
    }
}