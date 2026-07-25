using UnityEngine;

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
    public GameObject[] enemyPrefabs;
    public Transform[] spawnPoints;
    public int lootSpawnCount = 15;
    public int enemySpawnBaseCount = 5;

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
            // 接收三个维度的数值扣除结果
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
        foreach (var loot in FindObjectsByType<InteractableLoot>(FindObjectsSortMode.None)) Destroy(loot.gameObject);
        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) Destroy(enemy.gameObject);

        if (spawnPoints == null || spawnPoints.Length == 0) return;

        for (int i = 0; i < lootSpawnCount; i++)
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (survivalLootPrefabs.Length > 0 && survivalLootPrefabs[0] != null)
            {
                GameObject prefab = survivalLootPrefabs[0];
                Vector2 offset = Random.insideUnitCircle * 2f;
                Instantiate(prefab, (Vector2)sp.position + offset, Quaternion.identity);
            }
        }

        int enemyCount = enemySpawnBaseCount + (currentDay / 2);
        for (int i = 0; i < enemyCount; i++)
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