using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ImageEffectsHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    #region Variables
    [Header("Click Effects")]
    [Tooltip("Enable sprite swap effect on click.")]
    public bool enableClickSpriteSwap;
    [Tooltip("Sprite to swap when clicked.")]
    public Sprite clickSwapSprite;

    [Tooltip("Enable color change effect on click.")]
    public bool enableClickColorChange;
    [Tooltip("Color to change when clicked.")]
    public Color clickTargetColor = Color.red;

    [Header("Hover Effects")]
    [Tooltip("Enable sprite swap effect on hover.")]
    public bool enableHoverSpriteSwap;
    [Tooltip("Sprite to swap when hovered.")]
    public Sprite hoverSwapSprite;

    [Tooltip("Enable sprite fade effect on hover.")]
    public bool enableHoverSpriteFade;
    [Tooltip("Fade value (0 = fully transparent, 1 = fully visible).")]
    [Range(0, 1)] public float hoverFadeAmount = 0.5f;

    [Tooltip("Enable color change effect on hover.")]
    public bool enableHoverColorChange;
    [Tooltip("Color to change when hovered.")]
    public Color hoverTargetColor = Color.blue;

    private Image imageComponent;
    private Sprite originalSprite;
    private Color originalColor;
    private static ImageEffectsHandler lastClickedButton;
    private bool isClicked = false;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        imageComponent = GetComponent<Image>();
        if (imageComponent)
        {
            originalSprite = imageComponent.sprite;
            originalColor = imageComponent.color;
        }
    }
    #endregion

    #region Pointer Events
    public void OnPointerDown(PointerEventData eventData)
    {
        ResetLastClickedButton();
        ApplyClickEffects();
        lastClickedButton = this;
        isClicked = true;
    }

    public void OnPointerUp(PointerEventData eventData) { }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isClicked)
        {
            ApplyHoverEffects();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isClicked)
        {
            ResetHoverEffects();
        }
    }
    #endregion

    #region Effect Methods
    private void ApplyClickEffects()
    {
        if (enableClickSpriteSwap) SwapSprite(clickSwapSprite);
        if (enableClickColorChange) ChangeColor(clickTargetColor);
    }

    private void ApplyHoverEffects()
    {
        if (enableHoverSpriteSwap) SwapSprite(hoverSwapSprite);
        if (enableHoverSpriteFade) FadeSprite(hoverFadeAmount);
        if (enableHoverColorChange) ChangeColor(hoverTargetColor);
    }

    private void ResetHoverEffects()
    {
        if (enableHoverSpriteSwap || enableHoverSpriteFade || enableHoverColorChange)
        {
            imageComponent.sprite = originalSprite;
            imageComponent.color = originalColor;
        }
    }

    private void ResetToInitialState()
    {
        imageComponent.sprite = originalSprite;
        imageComponent.color = originalColor;
        isClicked = false;
    }

    private void ResetLastClickedButton()
    {
        if (lastClickedButton && lastClickedButton != this)
        {
            lastClickedButton.ResetToInitialState();
        }
    }

    private void SwapSprite(Sprite newSprite)
    {
        if (imageComponent && newSprite)
            imageComponent.sprite = newSprite;
    }

    private void FadeSprite(float fadeValue)
    {
        if (imageComponent)
        {
            Color fadedColor = imageComponent.color;
            fadedColor.a = fadeValue;
            imageComponent.color = fadedColor;
        }
    }

    private void ChangeColor(Color newColor)
    {
        if (imageComponent)
            imageComponent.color = newColor;
    }
    #endregion
}
