using UnityEngine;
using UnityEngine.UI;

public class PermanentButtonColor : MonoBehaviour
{
    public Color clickedColor = Color.green;

    private Button btn;
    private bool isClicked = false;

    void Start()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(SetClickedColor);
    }

    void SetClickedColor()
    {
        if (isClicked) return; // already clicked, do nothing

        ColorBlock cb = btn.colors;
        cb.normalColor = clickedColor;
        btn.colors = cb;

        isClicked = true;
    }
}