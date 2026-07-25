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

            foreach (var item in allItems)
            {
                if (item.quality == "Common") item.itemColor = Color.gray;
                else if (item.quality == "Rare") item.itemColor = new Color(0.2f, 0.6f, 1f);
                else if (item.quality == "Legendary") item.itemColor = new Color(1f, 0.8f, 0f);

                // 【正式版加载逻辑】：严格按照物品名称精准匹配图片
                item.iconSprite = Resources.Load<Sprite>($"ItemIcons/{item.itemName}");

                // 如果没找到同名图片，在控制台发出警告，方便排查错别字或漏传的图片
                if (item.iconSprite == null)
                {
                    Debug.LogWarning($"[Icon Missing] 找不到图片: {item.itemName}，请检查 Resources/ItemIcons/ 文件夹下是否有同名图片！");
                }
            }
            Debug.Log($"Successfully loaded JSON item database, containing {allItems.Count} items!");
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