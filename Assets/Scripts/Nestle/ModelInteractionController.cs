using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ModelInteractionController : MonoBehaviour
{
    [Header("Rotation Limits")]
    public float minX = -45f;
    public float maxX = 45f;

    public float minY = -180f;
    public float maxY = 180f;

    [Header("Smooth Rotation")]
    public float rotationSmoothSpeed = 5f;

    private float currentX;
    private float currentY;

    private float targetX;
    private float targetY;

    [Header("UI References")]
    public Scrollbar horizontalScrollbar; // Left-Right (Y axis)
    public Scrollbar verticalScrollbar;   // Up-Down (X axis)

    [Header("Zoom Settings")]
    public float zoomStep = 0.2f;
    public float minScale = 0.5f;
    public float maxScale = 2f;

    public GameObject model;
    void Start()
    {
        Vector3 angles = model.transform.localEulerAngles;

        currentX = angles.x;
        currentY = angles.y;

        targetX = currentX;
        targetY = currentY;

       if (horizontalScrollbar != null)
        {
             horizontalScrollbar.value = 0.5f;
        }

       if (verticalScrollbar != null)
        {
             verticalScrollbar.value = 1f;
        }
           
        // Also reset target to center position
        targetY = Mathf.Lerp(minY, maxY, 0.5f);
        targetX = Mathf.Lerp(minX, maxX, 0.5f);

        currentY = targetY;
        currentX = targetX;

         model.transform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
    }

    void Update()
    {
        HandleScrollbarInput();
        SmoothRotate();

    }

    // ---------------- SCROLLBAR INPUT ----------------

    void HandleScrollbarInput()
    {
        if (horizontalScrollbar != null)
        {
            targetY = Mathf.Lerp(minY, maxY, horizontalScrollbar.value);
        }

        if (verticalScrollbar != null)
        {
            targetX = Mathf.Lerp(minX, maxX, verticalScrollbar.value);
        }
    }

    // ---------------- SMOOTH ROTATION ----------------

    void SmoothRotate()
    {
        currentX = Mathf.Lerp(currentX, targetX, Time.deltaTime * rotationSmoothSpeed);
        currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * rotationSmoothSpeed);

        currentX = ClampAngle(currentX, minX, maxX);
        currentY = ClampAngle(currentY, minY, maxY);

         model.transform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
    }

    float ClampAngle(float angle, float min, float max)
    {
        if (angle > 180) angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }

    // ---------------- ZOOM BUTTONS ----------------

    public void ZoomIn()
    {
        Zoom(zoomStep);
    }

    public void ZoomOut()
    {
        Zoom(-zoomStep);
    }

    void Zoom(float increment)
    {
        Vector3 newScale =  model.transform.localScale + Vector3.one * increment;

        float clamped = Mathf.Clamp(newScale.x, minScale, maxScale);

         model.transform.localScale = new Vector3(clamped, clamped, clamped);
    }

 
}