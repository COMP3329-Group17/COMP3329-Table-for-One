// This is the brain of inventory feature, Create Empty in Unity Hierarchy, and put the script in it. 
using UnityEngine;
//Library for "List"
using System.Collections.Generic;

public class InventoryMG : MonoBehaviour
{
    // Create a list to store the player's item 
    public List<ItemData> items = new List<ItemData>();

    // Function for adding or picking up an item
    public void AddItem(ItemData item)
    {
        // put the item into the list
        items.Add(item);
        Debug.Log("Added item: " + item.itemName);
    }

    // Function for removing an item
    public void RemoveItem(ItemData item)
    {
        if (items.Contains(item))
        {
            // remove the item into the list
            items.Remove(item);
            Debug.Log("Removed item: " + item.itemName);
        }
        else
        {
            Debug.Log("Item not found: " + item.itemName);
        }
    }

    public void DisplayInventory()
    {
        Debug.Log("Inventory:");
        foreach (var item in items)
        {
            Debug.Log("- " + item.itemName);
        }
    }
}