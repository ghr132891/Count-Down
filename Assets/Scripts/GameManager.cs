using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 单例模式，方便其他脚本全局调用
    public static GameManager Instance { get; private set; }

    [Header("Day & Time Settings")]
    public int currentDay = 1;
    public float dayDuration = 180f; // 每一天的总时长（秒，例如 180秒 = 3分钟）
    private float timeRemaining;

    [Header("Map Refresh Settings")]
    public GameObject[] survivalLootPrefabs; // 生活必需品物资库（水、食物等预制体）
    public GameObject[] enemyPrefabs;        // 怪物预制体库
    public Transform[] spawnPoints;          // 地图上的随机刷新点

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 游戏启动，开启第一天
        StartNewDay();
    }

    private void Update()
    {
        // 倒计时流逝
        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            // 时间耗尽，强制推进天数并刷新地图
            EndDayAndRefreshMap();
        }
    }

    // 开始新的一天
    private void StartNewDay()
    {
        timeRemaining = dayDuration;
        Debug.Log($"<color=yellow>=== 第 {currentDay} 天开始！请抓紧时间搜寻生存物资 ===</color>");

        // 刷新地图：清理残余，重新生成食物、水和怪物
        RefreshMap();
    }

    // 时间结束，强制清场，进入下一天
    private void EndDayAndRefreshMap()
    {
        Debug.Log("<color=red>=== 时间耗尽！天黑或到了强制回收点，未运回的物资已丢失，进入新的一天... ===</color>");

        // 天数递进
        currentDay++;

        // 开启新的一天
        StartNewDay();
    }

    // 重置并刷新地图
    private void RefreshMap()
    {
        // 1. 彻底清理地图上所有残留的拾取物 (带有 InteractableLoot 脚本的物体)
        InteractableLoot[] leftLoots = FindObjectsByType<InteractableLoot>(FindObjectsSortMode.None);
        foreach (var loot in leftLoots)
        {
            Destroy(loot.gameObject);
        }

        // 2. 彻底清理地图上所有残留的怪物
        EnemyController[] leftEnemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (var enemy in leftEnemies)
        {
            Destroy(enemy.gameObject);
        }

        // 如果没有配好刷新点，直接返回
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        // 3. 随机生成生活物资（水、食物等）
        int lootCount = Random.Range(6, 12); // 每天随机刷 6 到 11 个物资
        for (int i = 0; i < lootCount; i++)
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];

            if (survivalLootPrefabs.Length > 0)
            {
                GameObject lootPrefab = survivalLootPrefabs[Random.Range(0, survivalLootPrefabs.Length)];
                Vector2 randomOffset = Random.insideUnitCircle * 2.5f; // 稍微偏移，避免全叠在一个点
                Instantiate(lootPrefab, (Vector2)sp.position + randomOffset, Quaternion.identity);
            }
        }

        // 4. 随着天数增加，怪物逐渐增多（难度递增）
        int enemyCount = 1 + (currentDay / 2); // 比如第1-2天2只，第3-4天3只...
        for (int i = 0; i < enemyCount; i++)
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];

            if (enemyPrefabs.Length > 0)
            {
                GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                Instantiate(enemyPrefab, sp.position, Quaternion.identity);
            }
        }
    }

    // 屏幕上方实时显示天数和倒计时 UI
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 22;
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;

        // 格式化时间为 mm:ss
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        string timeString = string.Format("{0:00}:{1:00}", minutes, seconds);

        // 绘制顶部中心信息框
        GUI.Box(new Rect(Screen.width / 2 - 110, 15, 220, 40), "");
        GUI.Label(new Rect(Screen.width / 2 - 100, 20, 200, 30), $"第 {currentDay} 天 | 剩: {timeString}", style);
    }
}