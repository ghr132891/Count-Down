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

// 路径: Assets/Scripts/GameManager.cs
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

    [Header("Manual Chest Spawn Setup")]
    public List<ChestSpawnConfig> chestSpawnConfigs = new List<ChestSpawnConfig>();

    [Header("Enemy Dynamic Spawns")]
    [Range(1, 30)] public int baseEnemyCount = 5;
    [Range(0f, 5f)] public float enemyIncreasePerDay = 0.5f;

    [Header("Core References")]
    public DailySummaryUI summaryUI;
    public PlayerInventory stashInventory;
    public PlayerController player;

    [Tooltip("玩家死亡复活时的具体位置（拖入避难所里的一个空物体）")]
    public Transform shelterSpawnPoint; // <--- 关键：这就是复活位置！

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
        if (currentDay > maxDays) return;

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

    // --- 【死亡与复活核心逻辑】 ---

    public void PlayerDied()
    {
        isDayActive = false;
        isTimerRunning = false;

        // 死亡时，直接呼出死亡 UI 界面，等待玩家点击
        if (UIManager.Instance != null) UIManager.Instance.ShowDeathPanel();
    }

    public void ExecutePlayerRespawn()
    {
        // 1. 扣除生存物资作为死亡惩罚
        if (SurvivalManager.Instance != null) SurvivalManager.Instance.PenalizeDeath();

        // 2. 将玩家强行拉回避难所的“复活点”位置，并恢复满状态
        if (player != null && shelterSpawnPoint != null)
        {
            player.transform.position = shelterSpawnPoint.position;
            player.RestoreFullStats();
        }
        else
        {
            Debug.LogError("复活失败：GameManager 中没有绑定 Player 或是 Shelter Spawn Point！");
        }

        isPlayerInShelter = true;

        // 3. 弹出夜晚结算界面
        ShowSummary();
    }

    // ---------------------------------

    private void ShowSummary()
    {
        if (summaryUI != null) summaryUI.ShowEveningPanel(stashInventory);
    }

    public void AdvanceToNextDay()
    {
        currentDay++;
        StartNewDay();
    }

    private void RefreshMap()
    {
        foreach (var loot in FindObjectsByType<InteractableLoot>(FindObjectsSortMode.None)) Destroy(loot.gameObject);
        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) Destroy(enemy.gameObject);
        foreach (var chest in FindObjectsByType<ChestController>(FindObjectsSortMode.None)) Destroy(chest.gameObject);

        if (chestPrefab != null)
        {
            foreach (var config in chestSpawnConfigs)
            {
                if (config.spawnLocation == null) continue;

                for (int i = 0; i < config.normalChestCount; i++)
                {
                    Vector2 offset = config.spawnRadius > 0 ? Random.insideUnitCircle * config.spawnRadius : Vector2.zero;
                    Vector3 pos = config.spawnLocation.position + (Vector3)offset;
                    GameObject chestObj = Instantiate(chestPrefab, pos, Quaternion.identity);
                    ChestController chest = chestObj.GetComponent<ChestController>();
                    if (chest != null) chest.SetupChest(ChestController.ChestType.Normal);
                }

                for (int i = 0; i < config.preciousChestCount; i++)
                {
                    Vector2 offset = config.spawnRadius > 0 ? Random.insideUnitCircle * config.spawnRadius : Vector2.zero;
                    Vector3 pos = config.spawnLocation.position + (Vector3)offset;
                    GameObject chestObj = Instantiate(chestPrefab, pos, Quaternion.identity);
                    ChestController chest = chestObj.GetComponent<ChestController>();
                    if (chest != null) chest.SetupChest(ChestController.ChestType.Precious);
                }
            }
        }

        if (enemySpawnPoints != null && enemySpawnPoints.Length > 0)
        {
            int todayEnemyCount = baseEnemyCount + Mathf.FloorToInt(currentDay * enemyIncreasePerDay);
            for (int i = 0; i < todayEnemyCount; i++)
            {
                Transform sp = enemySpawnPoints[Random.Range(0, enemySpawnPoints.Length)];
                if (enemyPrefabs.Length > 0)
                {
                    GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                    Instantiate(prefab, sp.position, Quaternion.identity);
                }
            }
        }
    }
}