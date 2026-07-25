using UnityEngine;

// 路径: Assets/Scripts/Item/InteractableLoot.cs
[RequireComponent(typeof(Collider2D))]
public class InteractableLoot : MonoBehaviour
{
    [Header("显示设置")]
    public float groundScale = 1.2f;

    private ItemData itemToGive;
    private ItemInstance currentInstance;
    private bool canInteract = false;
    private PlayerInventory playerInventory;

    // 【新增】标记是否已经被外部（比如丢弃功能）初始化过了
    private bool isInitialized = false;

    // 【新增】供外部调用：将背包里的物品丢到地上
    public void SetupDroppedItem(ItemInstance droppedInstance)
    {
        itemToGive = droppedInstance.data;
        currentInstance = droppedInstance; // 继承旋转状态等实例数据
        isInitialized = true;
        ApplyVisuals();
    }

    private void Start()
    {
        // 如果没有被丢弃功能初始化，就走默认的随机生成逻辑
        if (!isInitialized && ItemDatabase.Instance != null)
        {
            itemToGive = ItemDatabase.Instance.GetRandomLoot();
            currentInstance = new ItemInstance(itemToGive);
            isInitialized = true;
            ApplyVisuals();
        }
    }

    // 提取公共视觉刷新方法
    private void ApplyVisuals()
    {
        transform.localScale = new Vector3(groundScale, groundScale, 1f);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && itemToGive != null && itemToGive.iconSprite != null)
        {
            sr.sprite = itemToGive.iconSprite;
        }

        // 【细节优化】如果丢弃前物品是旋转状态，地上的模型也转 90 度
        if (currentInstance != null && currentInstance.isRotated)
        {
            transform.localEulerAngles = new Vector3(0, 0, 90f);
        }
    }

    private void Update()
    {
        if (canInteract && currentInstance != null)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                currentInstance.isRotated = !currentInstance.isRotated;
                // 同步旋转地上的贴图
                transform.localEulerAngles = currentInstance.isRotated ? new Vector3(0, 0, 90f) : Vector3.zero;
                Debug.Log($"Item rotated: {currentInstance.Width}x{currentInstance.Height}");
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                if (playerInventory != null)
                {
                    bool success = playerInventory.AutoAddItem(currentInstance);
                    if (success) Destroy(gameObject);
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            playerInventory = other.GetComponent<PlayerInventory>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            playerInventory = null;
        }
    }

    private void OnGUI()
    {
        if (canInteract && currentInstance != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            GUIStyle style = new GUIStyle();
            style.fontSize = 16;
            style.normal.textColor = currentInstance.data.itemColor;
            style.fontStyle = FontStyle.Bold;

            // 【核心修改】去掉了 {currentInstance.data.quality} 前缀
            string prompt = $"[F] Pick up {currentInstance.data.itemName} ({currentInstance.Width}x{currentInstance.Height})\n[R] Rotate Item";

            GUI.Label(new Rect(screenPos.x - 40, Screen.height - screenPos.y - 60, 250, 50), prompt, style);
        }
    }
}