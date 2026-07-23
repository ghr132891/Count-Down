using UnityEngine;

// 路径: Assets/Scripts/Item/InteractableLoot.cs
[RequireComponent(typeof(Collider2D))]
public class InteractableLoot : MonoBehaviour
{
    [Header("Loot Data")]
    public ItemData itemToGive;

    // 当前地上的这个物品实例
    private ItemInstance currentInstance;

    private bool canInteract = false;
    private PlayerInventory playerInventory;

    private void Start()
    {
        // 游戏开始时，基于模板生成一个独立的实例
        if (itemToGive != null)
        {
            currentInstance = new ItemInstance(itemToGive);
        }
    }

    private void Update()
    {
        if (canInteract && currentInstance != null)
        {
            // --- 新增：按 R 键旋转物品 ---
            if (Input.GetKeyDown(KeyCode.R))
            {
                currentInstance.isRotated = !currentInstance.isRotated;
                Debug.Log($"【提示】物品已旋转，当前需要空间: {currentInstance.Width}x{currentInstance.Height}");
            }

            // 按 F 键拾取
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (playerInventory != null)
                {
                    // 传入带有当前旋转状态的实例
                    bool success = playerInventory.AutoAddItem(currentInstance);
                    if (success)
                    {
                        Destroy(gameObject);
                    }
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
            style.normal.textColor = Color.green;
            style.fontStyle = FontStyle.Bold;

            // 实时显示当前的长宽状态，让你知道塞进包里会是横的还是竖的
            string prompt = $"[F] 拾取 ({currentInstance.Width}x{currentInstance.Height})\n[R] 旋转";
            GUI.Label(new Rect(screenPos.x - 40, Screen.height - screenPos.y - 60, 150, 50), prompt, style);
        }
    }
}