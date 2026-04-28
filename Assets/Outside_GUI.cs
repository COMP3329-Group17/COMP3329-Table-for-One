using UnityEngine;

public class EndGameUI : MonoBehaviour
{
    [Header("Display Settings")]
    public string instructionsText = "It's over... (Isn't it?)";
    public Color textColor = Color.red;

    void OnGUI()
    {
        // 1. Styling the Box
        GUI.skin.box.fontSize = 42;
        GUI.skin.box.normal.textColor = textColor;
        GUI.skin.box.alignment = TextAnchor.MiddleCenter;

        // 2. Sizing and Positioning
        float boxWidth = 1500f;
        float boxHeight = 160f;
        float boxX = (Screen.width - boxWidth) / 2;
        float boxY = 120f;

        // 3. Draw the GUI Box
        GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), instructionsText);

        // Bonus: If you want to make it look glitchy/scary, 
        // you can draw it twice with a tiny offset in a different color
        GUI.skin.box.normal.textColor = new Color(0, 0, 0, 0.5f); // Shadow
        GUI.Box(new Rect(boxX + 5, boxY + 5, boxWidth, boxHeight), instructionsText);
    }
}