using UnityEngine;

// 路径: Assets/Scripts/Item/ChestController.cs
[RequireComponent(typeof(Collider2D))]
public class ChestController : MonoBehaviour
{
    [Header("Chest Settings")]
    [Tooltip("开启宝箱需要的读条时间（秒）")]
    public float openDuration = 1.5f;

    [Tooltip("第1天开启宝箱的基础物品数量")]
    public int baseItemCount = 2;

    [Tooltip("每天增加的爆出物品数量")]
    public float itemIncreasePerDay = 0.5f;

    [Tooltip("物品数量的随机波动范围 (例如设为1，如果计算出该掉3个，实际会掉2~4个)")]
    public int fluctuation = 1;

    private float currentOpenTime = 0f;
    private bool isPlayerNearby = false;
    private bool isOpening = false;

    private void Update()
    {
        if (isPlayerNearby)
        {
            // 长按 F 键开箱
            if (Input.GetKey(KeyCode.F))
            {
                isOpening = true;
                currentOpenTime += Time.deltaTime;

                // 读条满了，爆装备！
                if (currentOpenTime >= openDuration)
                {
                    OpenChest();
                }
            }
            else
            {
                // 松手则打断读条，进度清零
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

        // 1. 获取当前天数
        int currentDay = GameManager.Instance.currentDay;

        // 2. 核心逻辑：计算目标掉落数量 = 初始数量 + (天数 * 每天增加量)
        int targetLootCount = baseItemCount + Mathf.FloorToInt(currentDay * itemIncreasePerDay);

        // 3. 加入波动范围 (Fluctuation)
        int minLoot = Mathf.Max(1, targetLootCount - fluctuation); // 保证保底至少掉 1 个
        int maxLoot = targetLootCount + fluctuation + 1; // Random.Range 的上限是独占的，所以 +1
        int finalLootCount = Random.Range(minLoot, maxLoot);

        // 4. 生成物品并向四周散开
        GameObject lootPrefab = GameManager.Instance.survivalLootPrefabs[0];
        for (int i = 0; i < finalLootCount; i++)
        {
            // 在宝箱周围半径 1.5 的圆内随机取一个偏移点，让物品像爆出来一样散开，不会叠在一起
            Vector2 scatterOffset = Random.insideUnitCircle * 1.5f;
            Vector3 spawnPos = transform.position + new Vector3(scatterOffset.x, scatterOffset.y, 0f);

            Instantiate(lootPrefab, spawnPos, Quaternion.identity);
            // 此时生成的 InteractableLoot 会自动执行它自己 Start() 里的逻辑：去数据库抽一个随机物品图片换上
        }

        // 5. 箱子使命完成，销毁自身
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

    // 绘制简易的屏幕空间 UI，提示和读条
    private void OnGUI()
    {
        if (isPlayerNearby)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            float yOffset = Screen.height - screenPos.y;

            if (isOpening)
            {
                // 画读条背景（黑底）
                float barWidth = 100f;
                float barHeight = 12f;
                Rect bgRect = new Rect(screenPos.x - barWidth / 2, yOffset - 70, barWidth, barHeight);
                GUI.DrawTexture(bgRect, Texture2D.blackTexture);

                // 画读条进度（白条）
                float progress = Mathf.Clamp01(currentOpenTime / openDuration);
                Rect fgRect = new Rect(screenPos.x - barWidth / 2, yOffset - 70, barWidth * progress, barHeight);
                GUI.DrawTexture(fgRect, Texture2D.whiteTexture);
            }
            else
            {
                // 画提示文字
                GUIStyle style = new GUIStyle();
                style.fontSize = 16;
                style.normal.textColor = Color.yellow;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;

                GUI.Label(new Rect(screenPos.x - 100, yOffset - 80, 200, 50), "[Hold F] Open Chest", style);
            }
        }
    }
}