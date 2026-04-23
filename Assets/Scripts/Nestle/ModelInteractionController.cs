using UnityEngine;
using UnityEngine.EventSystems;

public class ModelInteractionController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 100f;

    public float minX = -45f;
    public float maxX = 45f;

    public float minY = -180f;
    public float maxY = 180f;

    private float currentX;
    private float currentY;

    private bool rotateLeft;
    private bool rotateRight;
    private bool rotateUp;
    private bool rotateDown;

    [Header("Zoom Settings")]
    public float zoomSpeed = 0.5f;
    public float minScale = 0.5f;
    public float maxScale = 2f;

    private bool isHovering = false;

    void Start()
    {
        Vector3 angles = transform.localEulerAngles;

        currentX = angles.x;
        currentY = angles.y;
    }

    void Update()
    {
        HandleRotation();
        HandleMouseZoom();
        HandleTouchZoom();
    }

    // ---------------- ROTATION ----------------

    void HandleRotation()
    {
        if (rotateLeft)
            currentY -= rotationSpeed * Time.deltaTime;

        if (rotateRight)
            currentY += rotationSpeed * Time.deltaTime;

        if (rotateUp)
            currentX -= rotationSpeed * Time.deltaTime;

        if (rotateDown)
            currentX += rotationSpeed * Time.deltaTime;

        ApplyRotation();
    }

    void ApplyRotation()
    {
        currentX = ClampAngle(currentX, minX, maxX);
        currentY = ClampAngle(currentY, minY, maxY);

        transform.localRotation = Quaternion.Euler(currentX, currentY, 0f);
    }

    float ClampAngle(float angle, float min, float max)
    {
        if (angle > 180) angle -= 360;
        return Mathf.Clamp(angle, min, max);
    }

    // ---------------- BUTTON EVENTS ----------------

    public void OnLeftDown() => rotateLeft = true;
    public void OnLeftUp() => rotateLeft = false;

    public void OnRightDown() => rotateRight = true;
    public void OnRightUp() => rotateRight = false;

    public void OnUpDown() => rotateUp = true;
    public void OnUpUp() => rotateUp = false;

    public void OnDownDown() => rotateDown = true;
    public void OnDownUp() => rotateDown = false;

    // ---------------- ZOOM ----------------

    void HandleMouseZoom()
    {
        if (!isHovering) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            Zoom(scroll * zoomSpeed);
        }
    }

    void HandleTouchZoom()
    {
        if (Input.touchCount != 2) return;

        Touch t1 = Input.GetTouch(0);
        Touch t2 = Input.GetTouch(1);

        Vector2 prevPos1 = t1.position - t1.deltaPosition;
        Vector2 prevPos2 = t2.position - t2.deltaPosition;

        float prevMagnitude = (prevPos1 - prevPos2).magnitude;
        float currentMagnitude = (t1.position - t2.position).magnitude;

        float difference = currentMagnitude - prevMagnitude;

        Zoom(difference * zoomSpeed * 0.01f);
    }

    void Zoom(float increment)
    {
        Vector3 newScale = transform.localScale + Vector3.one * increment;

        float clamped = Mathf.Clamp(newScale.x, minScale, maxScale);

        transform.localScale = new Vector3(clamped, clamped, clamped);
    }

    // ---------------- HOVER ----------------

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Pointer entered");
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }
}