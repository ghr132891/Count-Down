using UnityEngine;
using System.Collections.Generic;

// 路径: Assets/Scripts/Maps/ShelterStashChest.cs
[RequireComponent(typeof(Collider2D))]
public class ShelterStashChest : MonoBehaviour
{
    [Header("UI 引用")]
    public InventoryGridUI playerBackpackUI;
    public InventoryGridUI stashUI;
    private bool isPlayerNearby = false;

    private void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (stashUI != null && playerBackpackUI != null)
            {
                bool targetState = !stashUI.IsOpen();
                stashUI.SetPanelActive(targetState);
                playerBackpackUI.SetPanelActive(targetState);
            }
        }

        // 一键存入所有物品
        if (isPlayerNearby && stashUI != null && stashUI.IsOpen() && Input.GetKeyDown(KeyCode.G))
        {
            DepositAllToStash();
        }
    }

    private void DepositAllToStash()
    {
        PlayerInventory pInv = playerBackpackUI.inventory;
        PlayerInventory sInv = stashUI.inventory;
        List<ItemInstance> itemsToMove = new List<ItemInstance>();

        foreach (var pItem in pInv.placedItems) itemsToMove.Add(pItem.instance);

        bool movedAny = false;
        foreach (var item in itemsToMove)
        {
            if (sInv.AutoAddItem(item))
            {
                pInv.RemoveItem(item);
                movedAny = true;
            }
        }

        if (movedAny)
        {
            pInv.uiManager.RefreshUI();
            sInv.uiManager.RefreshUI();
            Debug.Log("<color=green>All items deposited into stash!</color>");
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
            if (stashUI != null) stashUI.SetPanelActive(false);
        }
    }

    private void OnGUI()
    {
        if (isPlayerNearby)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            GUIStyle style = new GUIStyle();
            style.fontSize = 18;
            style.normal.textColor = Color.yellow;
            style.fontStyle = FontStyle.Bold;

            string prompt = (stashUI != null && stashUI.IsOpen()) ? "Press [E] to Close / [G] to Deposit All" : "Press [E] to Open Stash";

            GUI.Label(new Rect(screenPos.x - 80, Screen.height - screenPos.y - 70, 300, 30), prompt, style);
        }
    }
}