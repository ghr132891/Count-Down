using UnityEngine;

// 专门管理 60s 类型的生存状态数值
public class SurvivalManager : MonoBehaviour
{
    public static SurvivalManager Instance { get; private set; }

    [Header("Survival Stats")]
    public int food = 10;
    public int water = 10;
    public int shelterDurability = 50;
    public int injuryCount = 0; // 永久累积，不可重置

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 每天结算时调用（可被剧情选项或 GameManager 触发）
    public void ProcessDailyConsumption()
    {
        food -= 3;
        water -= 2;
        shelterDurability -= Random.Range(5, 11); // 每天僵尸攻击扣减 5~10

        CheckDeathConditions();
    }

    private void CheckDeathConditions()
    {
        if (food <= 0) Debug.Log("【游戏结束】饥饿死亡 (B1)");
        if (water <= 0) Debug.Log("【游戏结束】脱水死亡 (B1)");
        if (shelterDurability <= 0) Debug.Log("【游戏结束】僵尸破门 (B3)");
        if (injuryCount >= 5) Debug.Log("【游戏结束】累计受伤感染 (B4)");
    }

    // 供剧情选项调用的修改数值接口
    public void ModifyStats(int foodChange, int waterChange, int durabilityChange)
    {
        food = Mathf.Max(0, food + foodChange);
        water = Mathf.Max(0, water + waterChange);
        shelterDurability = Mathf.Clamp(shelterDurability + durabilityChange, 0, 100);
    }
}