using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ChestSpawnConfig
{
    public Transform spawnLocation;
    public int normalChestCount = 1;
    public int preciousChestCount = 0;
    public float spawnRadius = 2f;
}

// Path: Assets/Scripts/GameManager.cs
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Loop Settings")]
    public int currentDay = 1;
    public int maxDays = 30;
    public float dayDuration = 480f;
    public float currentTime;
    public bool isDayActive = false;
    public bool isTimerRunning = false;
    public bool isPlayerInShelter = true;

    [Header("Spawn Controllers")]
    public GameObject[] survivalLootPrefabs;
    public GameObject chestPrefab;
    public GameObject[] enemyPrefabs;
    public Transform[] enemySpawnPoints;

    [Header("Spawn Safety & Obstacle Check")]
    [Tooltip("在此勾选不可以生成物体的图层（如 Obstacle 建筑物、AirWall 空气墙）")]
    public LayerMask obstacleLayer; // 【新增】障碍物与地图边缘图层
    public float enemySpawnRadius = 3f; // 【新增】敌人随机刷新的扩散半径

    [Header("Manual Chest Spawn Setup")]
    public List<ChestSpawnConfig> chestSpawnConfigs = new List<ChestSpawnConfig>();

    [Header("Enemy Dynamic Spawns")]
    [Range(1, 30)] public int baseEnemyCount = 5;
    [Range(0f, 5f)] public float enemyIncreasePerDay = 0.5f;

    [Header("Core References")]
    public DailySummaryUI summaryUI;
    public PlayerInventory stashInventory;
    public PlayerController player;
    public Transform shelterSpawnPoint;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        StartNewDay();
    }

    private void Update()
    {
        if (isDayActive && isTimerRunning)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                currentTime = 0;
                isDayActive = false;
                isTimerRunning = false;
                if (isPlayerInShelter) ShowSummary();
                else PlayerDied();
            }
        }
    }

    public void StartNewDay()
    {
        if (currentDay > maxDays)
        {
            Debug.Log("<color=yellow>Game Cleared! You survived all days!</color>");
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowVictoryPanel();
            }
            return;
        }

        currentTime = dayDuration;
        isDayActive = true;
        isTimerRunning = false;
        isPlayerInShelter = true;

        if (player != null) player.RestoreFullStats();
        RefreshMap();

        if (currentDay > 1 && SurvivalManager.Instance != null)
        {
            SurvivalManager.Instance.ProcessDailyDeduction(currentDay, out float foodDed, out float waterDed, out float durDed);
            if (summaryUI != null) summaryUI.ShowMorningPanel(currentDay, foodDed, waterDed, durDed);
        }
        else
        {
            if (summaryUI != null) summaryUI.ShowMorningPanel(currentDay, 0, 0, 0);
        }
    }

    public void StartTimer()
    {
        if (isDayActive && !isTimerRunning)
        {
            isTimerRunning = true;
            Debug.Log("First time leaving shelter, 8-minute countdown started!");
        }
    }

    public void PlayerDied()
    {
        isDayActive = false;
        isTimerRunning = false;
        if (UIManager.Instance != null) UIManager.Instance.ShowDeathPanel();
    }

    public void ExecutePlayerRespawn()
    {
        if (SurvivalManager.Instance != null) SurvivalManager.Instance.PenalizeDeath();

        if (player != null && shelterSpawnPoint != null)
        {
            player.transform.position = shelterSpawnPoint.position;
            player.RestoreFullStats();
        }
        else
        {
            Debug.LogError("GameManager: Player or Shelter Spawn Point missing!");
        }

        isPlayerInShelter = true;
        ShowSummary();
    }

    private void ShowSummary()
    {
        if (summaryUI != null) summaryUI.ShowEveningPanel(stashInventory);
    }

    public void AdvanceToNextDay()
    {
        currentDay++;
        StartNewDay();
    }

    // --- 【核心新增：获取非障碍物区域的安全坐标】 ---
    public Vector3 GetValidSpawnPosition(Vector3 center, float maxRadius, float checkRadius = 0.6f, int maxAttempts = 20)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 randomOffset = (maxRadius > 0) ? Random.insideUnitCircle * maxRadius : Vector2.zero;
            Vector3 testPos = center + (Vector3)randomOffset;

            // 检测目标位置周围 checkRadius 范围内是否有障碍物/空气墙
            if (obstacleLayer == 0 || !Physics2D.OverlapCircle(testPos, checkRadius, obstacleLayer))
            {
                return testPos; // 找到无障碍的安全坐标
            }
        }

        // 如果尝试 20 次都抽到墙里，退回初始中心点
        return center;
    }

    private void RefreshMap()
    {
        foreach (var loot in FindObjectsByType<InteractableLoot>(FindObjectsSortMode.None)) Destroy(loot.gameObject);
        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) Destroy(enemy.gameObject);
        foreach (var chest in FindObjectsByType<ChestController>(FindObjectsSortMode.None)) Destroy(chest.gameObject);

        // 1. 刷新宝箱（带安全碰撞检测）
        if (chestPrefab != null)
        {
            foreach (var config in chestSpawnConfigs)
            {
                if (config.spawnLocation == null) continue;

                for (int i = 0; i < config.normalChestCount; i++)
                {
                    Vector3 pos = GetValidSpawnPosition(config.spawnLocation.position, config.spawnRadius, 0.6f);
                    GameObject chestObj = Instantiate(chestPrefab, pos, Quaternion.identity);
                    ChestController chest = chestObj.GetComponent<ChestController>();
                    if (chest != null) chest.SetupChest(ChestController.ChestType.Normal);
                }

                for (int i = 0; i < config.preciousChestCount; i++)
                {
                    Vector3 pos = GetValidSpawnPosition(config.spawnLocation.position, config.spawnRadius, 0.6f);
                    GameObject chestObj = Instantiate(chestPrefab, pos, Quaternion.identity);
                    ChestController chest = chestObj.GetComponent<ChestController>();
                    if (chest != null) chest.SetupChest(ChestController.ChestType.Precious);
                }
            }
        }

        // 2. 刷新敌人（带安全碰撞检测，分散刷在草地上）
        if (enemySpawnPoints != null && enemySpawnPoints.Length > 0)
        {
            int todayEnemyCount = baseEnemyCount + Mathf.FloorToInt(currentDay * enemyIncreasePerDay);
            for (int i = 0; i < todayEnemyCount; i++)
            {
                Transform sp = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
                if (enemyPrefabs.Length > 0)
                {
                    GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                    Vector3 pos = GetValidSpawnPosition(sp.position, enemySpawnRadius, 0.8f);
                    Instantiate(prefab, pos, Quaternion.identity);
                }
            }
        }
    }
}