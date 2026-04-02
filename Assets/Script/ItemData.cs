// leave it in  the Assets Window
using UnityEngine;
// Adds a custom button to your Right-Click Create Menu in the Project folder for us to create item
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]

// We could input the following infor for our item directly in Unity
public class ItemData : ScriptableObject
{
    public string itemName; // Item name
    [TextArea(3, 10)] // Gives you more room in the inspector
    public string description; // Item description
    public Sprite icon; // 2D icon in the inventory menu 
    public GameObject prefab; // The 3D model for the world/inspection

    //Continuou on ,if more information is required for an item 
}