using UnityEngine;

// 路径: Assets/Scripts/Item/ChestController.cs
[RequireComponent(typeof(Collider2D))]
public class ChestController : MonoBehaviour
{
    public enum ChestType { Normal, Precious }
    private ChestType currentType = ChestType.Normal;

    [Header("Visual Settings")]
    public float normalScale = 1.2f;
    public float preciousScale = 1.5f;

    [Header("Loot Settings")]
    public int baseItemCount = 2;
    public float itemIncreasePerDay = 0.5f;
    public int fluctuation = 1;

    [Header("Time Settings")]
    public float openDuration = 1.5f;

    private float currentOpenTime = 0f;
    private bool isPlayerNearby = false;
    private bool isOpening = false;

    public void SetupChest(ChestType assignedType)
    {
        currentType = assignedType;

        if (currentType == ChestType.Precious)
        {
            transform.localScale = new Vector3(preciousScale, preciousScale, 1f);
            openDuration *= 1.5f;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 0.9f, 0.5f);
        }
        else
        {
            transform.localScale = new Vector3(normalScale, normalScale, 1f);
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;
        }
    }

    private void Update()
    {
        if (isPlayerNearby)
        {
            if (Input.GetKey(KeyCode.F))
            {
                isOpening = true;
                currentOpenTime += Time.deltaTime;

                if (currentOpenTime >= openDuration)
                {
                    OpenChest();
                }
            }
            else
            {
                isOpening = false;
                currentOpenTime = 0f;
            }
        }
        else
        {
            isOpening = false;
            currentOpenTime = 0f;
        }
    }

    private void OpenChest()
    {
        if (GameManager.Instance == null || GameManager.Instance.survivalLootPrefabs.Length == 0) return;

        int currentDay = GameManager.Instance.currentDay;

        // 核心数量公式：基础数量 + (天数 * 每天增量)
        int targetLootCount = baseItemCount + Mathf.FloorToInt(currentDay * itemIncreasePerDay);

        // 【已删除数量翻倍逻辑，统一数量】

        int minLoot = Mathf.Max(1, targetLootCount - fluctuation);
        int maxLoot = targetLootCount + fluctuation + 1;
        int finalLootCount = Random.Range(minLoot, maxLoot);

        GameObject lootPrefab = GameManager.Instance.survivalLootPrefabs[0];

        // 范围依然保持区别，因为珍贵宝箱的体型大，散开远一点不穿模
        float scatterRadius = currentType == ChestType.Precious ? 2.5f : 1.5f;

        for (int i = 0; i < finalLootCount; i++)
        {
            Vector2 scatterOffset = Random.insideUnitCircle * scatterRadius;
            Vector3 spawnPos = transform.position + new Vector3(scatterOffset.x, scatterOffset.y, 0f);

            // 实例化空掉落物
            GameObject lootObj = Instantiate(lootPrefab, spawnPos, Quaternion.identity);

            // 【核心修改】精准控制掉落物品的品质
            InteractableLoot lootScript = lootObj.GetComponent<InteractableLoot>();
            if (lootScript != null && ItemDatabase.Instance != null)
            {
                // 告诉数据库，我们需不需要抽取高爆率的极品装备
                bool isPreciousChest = (currentType == ChestType.Precious);
                ItemData rolledData = ItemDatabase.Instance.GetRandomLoot(isPreciousChest);

                // 将抽到的高级数据直接灌注给地上的掉落物
                lootScript.SetupDroppedItem(new ItemInstance(rolledData));
            }
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) isPlayerNearby = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) isPlayerNearby = false;
    }

    private void OnGUI()
    {
        if (isPlayerNearby)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            float yOffset = Screen.height - screenPos.y;

            if (isOpening)
            {
                float barWidth = currentType == ChestType.Precious ? 120f : 100f;
                float barHeight = 12f;

                Rect bgRect = new Rect(screenPos.x - barWidth / 2, yOffset - 70, barWidth, barHeight);
                GUI.DrawTexture(bgRect, Texture2D.blackTexture);

                float progress = Mathf.Clamp01(currentOpenTime / openDuration);
                Rect fgRect = new Rect(screenPos.x - barWidth / 2, yOffset - 70, barWidth * progress, barHeight);

                Color oldColor = GUI.color;
                GUI.color = currentType == ChestType.Precious ? new Color(1f, 0.8f, 0f) : Color.white;
                GUI.DrawTexture(fgRect, Texture2D.whiteTexture);
                GUI.color = oldColor;
            }
            else
            {
                GUIStyle style = new GUIStyle();
                style.fontSize = 16;
                style.normal.textColor = currentType == ChestType.Precious ? new Color(1f, 0.8f, 0f) : Color.yellow;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;

                string chestName = currentType == ChestType.Precious ? "Precious Chest" : "Chest";
                GUI.Label(new Rect(screenPos.x - 100, yOffset - 80, 200, 50), $"[Hold F] Open {chestName}", style);
            }
        }
    }
}