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
        int targetLootCount = baseItemCount + Mathf.FloorToInt(currentDay * itemIncreasePerDay);
        int minLoot = Mathf.Max(1, targetLootCount - fluctuation);
        int maxLoot = targetLootCount + fluctuation + 1;
        int finalLootCount = Random.Range(minLoot, maxLoot);

        GameObject lootPrefab = GameManager.Instance.survivalLootPrefabs[0];
        float scatterRadius = currentType == ChestType.Precious ? 2.5f : 1.5f;

        for (int i = 0; i < finalLootCount; i++)
        {
            // 利用 GameManager 的安全算法计算掉落物位置
            Vector3 spawnPos = GameManager.Instance.GetValidSpawnPosition(transform.position, scatterRadius, 0.4f);

            GameObject lootObj = Instantiate(lootPrefab, spawnPos, Quaternion.identity);
            InteractableLoot lootScript = lootObj.GetComponent<InteractableLoot>();
            if (lootScript != null && ItemDatabase.Instance != null)
            {
                bool isPreciousChest = (currentType == ChestType.Precious);
                ItemData rolledData = ItemDatabase.Instance.GetRandomLoot(isPreciousChest);
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