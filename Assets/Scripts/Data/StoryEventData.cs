using UnityEngine;
using System.Collections.Generic;

// 在项目右键菜单中添加创建该文件的快捷方式
[CreateAssetMenu(fileName = "New Story Event", menuName = "Game Data/Story Event")]
public class StoryEventData : ScriptableObject
{
    [Header("触发条件")]
    public string eventName = "事件名称";
    public bool isFixedEvent = false; // 是否为特定天数的固定剧情
    public int triggerDay = -1;       // 如果是固定剧情，第几天触发？
    [Range(0f, 1f)]
    public float probability = 0.5f;  // 如果是随机剧情，触发概率是多少？

    [Header("剧情内容")]
    [TextArea(3, 5)]
    public string description;

    [Header("玩家选项 (支持 1~2 个选项)")]
    public List<StoryOption> options;
}

[System.Serializable]
public class StoryOption
{
    public string optionText = "选项描述";

    [Header("生存物资影响")]
    public int foodChange;
    public int waterChange;
    public int durabilityChange;
    public int injuryChange;

    [Header("玩家自身属性影响")]
    public float hpChange;         // 直接加减当前血量
    public float maxHpChange;      // 削减/增加生命值上限
    public float maxStaminaChange; // 削减/增加体力上限
}