using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DiaryInteractable : MonoBehaviour
{
    public DiaryUI diaryUI;
    private bool isPlayerNearby = false;

    private void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (diaryUI != null)
            {
                diaryUI.ToggleDiary();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) isPlayerNearby = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (diaryUI != null && diaryUI.IsOpen()) diaryUI.ToggleDiary();
        }
    }

    private void OnGUI()
    {
        if (isPlayerNearby && diaryUI != null && !diaryUI.IsOpen())
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            GUIStyle style = new GUIStyle();
            style.fontSize = 18;
            style.normal.textColor = Color.yellow;
            style.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 70, 150, 30), "∞¥ [E] ‘ƒ∂¡»’º«", style);
        }
    }
}