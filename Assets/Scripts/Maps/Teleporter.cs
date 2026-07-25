using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 路径: Assets/Scripts/Maps/Teleporter.cs
public class Teleporter : MonoBehaviour
{
    [Header("传送设置")]
    public bool isShelterExit = true;
    public Transform targetDestination;

    [Header("UI 引用")]
    public GameObject confirmUIPanel;
    public Button btnAccept;
    public Button btnDecline;
    public TextMeshProUGUI promptText;

    private bool isPlayerNearby = false;
    private Transform playerTransform;

    private void Start()
    {
        if (confirmUIPanel != null) confirmUIPanel.SetActive(false);

        if (btnAccept != null) btnAccept.onClick.AddListener(TeleportPlayer);
        if (btnDecline != null) btnDecline.onClick.AddListener(CloseUI);
    }

    private void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (confirmUIPanel != null)
            {
                confirmUIPanel.SetActive(true);
                if (promptText != null)
                    promptText.text = isShelterExit ? "Are you sure you want to go out and explore?" : "Are you sure you want to end exploration and return to the shelter?";
            }
        }
    }

    private void TeleportPlayer()
    {
        // 必须判断玩家确实在当前这扇门旁边
        if (!isPlayerNearby) return;

        if (playerTransform != null && targetDestination != null)
        {
            Vector3 targetPos = targetDestination.position;
            targetPos.z = playerTransform.position.z;
            playerTransform.position = targetPos;

            // 告诉 GameManager 玩家出门还是回家了
            if (GameManager.Instance != null)
            {
                GameManager.Instance.isPlayerInShelter = !isShelterExit;

                // 如果玩家是从避难所出门，强制触发 GameManager 倒计时
                if (isShelterExit)
                {
                    GameManager.Instance.StartTimer();
                }
            }
        }
        CloseUI();
    }

    private void CloseUI()
    {
        if (confirmUIPanel != null) confirmUIPanel.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            playerTransform = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            playerTransform = null;
            CloseUI();
        }
    }

    private void OnGUI()
    {
        if (isPlayerNearby && (confirmUIPanel == null || !confirmUIPanel.activeSelf))
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            GUIStyle style = new GUIStyle();
            style.fontSize = 18;
            style.normal.textColor = Color.yellow;
            style.fontStyle = FontStyle.Bold;

            string msg = isShelterExit ? "Press [E] to Explore" : "Press [E] to Return to Shelter";
            GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 70, 250, 30), msg, style);
        }
    }
}