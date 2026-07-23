using UnityEngine;

// 路径: Assets/Scripts/Item/ItemData.cs
// 这个标签允许我们在 Unity 右键菜单中直接创建物品
[CreateAssetMenu(fileName = "New Item", menuName = "Game Data/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName = "未知物品";
    public Sprite icon;

    [Header("Grid Settings (网格背包设定)")]
    public int width = 1;  // 占据几列
    public int height = 1; // 占据几行

    // UI 显示用的颜色（纯测试用，方便我们在没有贴图时区分物品）
    public Color itemColor = Color.gray;
}