using UnityEngine;

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
    public GameObject[] survivalLootPrefabs; // 依然保留，宝箱开启时需要用到它
    public GameObject chestPrefab;           // 【新增】地图上生成的宝箱预制体
    public GameObject[] enemyPrefabs;
    public Transform[] spawnPoints;

    [Header("Dynamic Spawn Rates")]
    [Tooltip("第 1 天生成的宝箱总数")]
    [Range(3, 20)]
    public int baseChestCount = 6;

    [Tooltip("每天增加的宝箱数量")]
    [Range(0f, 2f)]
    public float chestIncreasePerDay = 0.5f;

    [Tooltip("第 1 天的基础敌人生成数量")]
    [Range(1, 30)]
    public int baseEnemyCount = 5;

    [Tooltip("每天增加的敌人数量")]
    [Range(0f, 5f)]
    public float enemyIncreasePerDay = 0.5f;

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
        if (currentDay > maxDays) return;

        currentTime = dayDuration;
        isDayActive = true;
        isTimerRunning = false;
        isPlayerInShelter = true;

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

        if (SurvivalManager.Instance != null) SurvivalManager.Instance.PenalizeDeath();

        if (player != null && shelterSpawnPoint != null)
        {
            player.transform.position = shelterSpawnPoint.position;
            player.currentHealth = player.maxHealth;
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

    private void RefreshMap()
    {
        // 销毁上一天遗留的掉落物、怪物，现在加上还要销毁上一天遗留的宝箱
        foreach (var loot in FindObjectsByType<InteractableLoot>(FindObjectsSortMode.None)) Destroy(loot.gameObject);
        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) Destroy(enemy.gameObject);
        foreach (var chest in FindObjectsByType<ChestController>(FindObjectsSortMode.None)) Destroy(chest.gameObject);

        if (spawnPoints == null || spawnPoints.Length == 0) return;

        // --- 核心修改：改为在地图上生成宝箱 ---
        int todayChestCount = baseChestCount + Mathf.FloorToInt(currentDay * chestIncreasePerDay);
        for (int i = 0; i < todayChestCount; i++)
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (chestPrefab != null)
            {
                Vector2 offset = Random.insideUnitCircle * 2f;
                Instantiate(chestPrefab, (Vector2)sp.position + offset, Quaternion.identity);
            }
        }

        // 生成敌人逻辑不变
        int todayEnemyCount = baseEnemyCount + Mathf.FloorToInt(currentDay * enemyIncreasePerDay);
        for (int i = 0; i < todayEnemyCount; i++)
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (enemyPrefabs.Length > 0)
            {
                GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                Instantiate(prefab, sp.position, Quaternion.identity);
            }
        }
    }
}