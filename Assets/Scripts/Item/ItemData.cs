using UnityEngine;
using System.Collections.Generic;

// Path: Assets/Scripts/Item/ItemData.cs

[System.Serializable]
public class ItemData
{
    public string itemName;
    public string quality;
    public string category;
    public int width;
    public int height;

    public float foodValue;
    public float waterValue;
    public float durabilityValue;

    public Color itemColor;

    // 【新增】用于在内存中存储加载好的图片
    // [System.NonSerialized] 告诉 Unity，这个不需要被 JSON 序列化（JSON 也存不了图）
    [System.NonSerialized]
    public Sprite iconSprite;
}

[System.Serializable]
public class ItemDataList
{
    public List<ItemData> items;
}