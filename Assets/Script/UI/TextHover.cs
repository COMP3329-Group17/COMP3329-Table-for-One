using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;


public class TextHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private GameObject highlight;
    [SerializeField] private TMP_ColorGradient redGradient;
    [SerializeField] private TMP_ColorGradient blueGradient;

    private void Start() {
        highlight.SetActive(false);
        text.enableVertexGradient = true;
        text.colorGradient = new VertexGradient(
            redGradient.topLeft,      
            redGradient.topRight,
            redGradient.bottomLeft,
            redGradient.bottomRight);

    }

    public void OnPointerEnter(PointerEventData data)
    {
        /* when button is hovered over:
            - change text to blue
            - show red highlight graphic
        */
        text.enableVertexGradient = true;
        text.colorGradient = new VertexGradient(
            blueGradient.topLeft,      
            blueGradient.topRight,
            blueGradient.bottomLeft,
            blueGradient.bottomRight);
        
        highlight.SetActive(true);
    }

    public void OnPointerExit(PointerEventData data)
    {
        /* return to default:
            - change text back to red
            - hide red highlight graphic
        */

        text.enableVertexGradient = true;
        text.colorGradient = new VertexGradient(
            redGradient.topLeft,      
            redGradient.topRight,
            redGradient.bottomLeft,
            redGradient.bottomRight);

        highlight.SetActive(false);
    }

}
