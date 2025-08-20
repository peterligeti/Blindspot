using UnityEngine;
using UnityEngine.UI;

public class ChangeCursorLookLogic : MonoBehaviour
{
    public Image cursorImage;
    public Vector2 cursorOffset = new Vector2(0, 0);
    
    void Start()
    {
        Cursor.visible = false;
        SetCursorSize(40f);
    }

    void Update()
    {
        Vector2 mousePosition = Input.mousePosition;
        Vector2 finalPosition = mousePosition + cursorOffset;
        
        cursorImage.rectTransform.position = finalPosition;
    }
    
    public void SetCursorSize(float size)
    {
        cursorImage.rectTransform.sizeDelta = new Vector2(size, size);
    }
}
