using UnityEngine;
using UnityEngine.EventSystems;

public class LookAround : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    public float sensitivity = 12f;

    [Header("Rotation Limits")]
    public float minY = -60f;
    public float maxY = 60f;

    [Header("Inertia Settings")]
    public float damping = 5f;

    // Shared flag to block camera when model is interacting
    public static bool isModelInteracting = false;

    private float rotationX = 0f;
    private float rotationY = 0f;

    private float velocityX = 0f;
    private float velocityY = 0f;

    private Vector2 lastMousePosition;
    private Vector2 lastTouchPosition;
    private bool isTouching = false;

    void Start()
    {
        Vector3 rot = transform.eulerAngles;
        rotationX = rot.y;
        rotationY = rot.x;
    }

    void Update()
    {
#if UNITY_WEBGL || UNITY_STANDALONE || UNITY_EDITOR
        HandleMouseLook();
#elif UNITY_ANDROID || UNITY_IOS
        HandleTouchLook();
#endif

        ApplyRotation();
    }

    void HandleMouseLook()
    {
        if (isModelInteracting) return;

        if (Input.GetMouseButtonDown(0))
        {
            // If clicking UI → stop inertia and block rotation
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                velocityX = 0f;
                velocityY = 0f;
                return;
            }

            lastMousePosition = Input.mousePosition;
            return;
        }

        if (Input.GetMouseButton(0))
        {
            // Extra safety: block if dragging over UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePosition;
            lastMousePosition = Input.mousePosition;

            float mouseX = mouseDelta.x * sensitivity * 0.01f;
            float mouseY = mouseDelta.y * sensitivity * 0.01f;

            velocityX += mouseX;
            velocityY += mouseY;
        }
    }

    void HandleTouchLook()
    {
        if (isModelInteracting) return;

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            {
                velocityX = 0f;
                velocityY = 0f;
                return;
            }

            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPosition = touch.position;
                isTouching = true;
                return;
            }
            else if (touch.phase == TouchPhase.Moved && isTouching)
            {
                Vector2 delta = touch.deltaPosition;

                float touchX = delta.x * sensitivity * 0.01f;
                float touchY = delta.y * sensitivity * 0.01f;

                velocityX += touchX;
                velocityY += touchY;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                isTouching = false;
            }
        }
    }

    void ApplyRotation()
    {
        // Block rotation when interacting with model
        if (isModelInteracting) return;

        // Apply inertia
        rotationX += velocityX;
        rotationY -= velocityY;

        // Clamp vertical only
        rotationY = Mathf.Clamp(rotationY, minY, maxY);

        // Smooth slowdown
        velocityX = Mathf.Lerp(velocityX, 0f, damping * Time.deltaTime);
        velocityY = Mathf.Lerp(velocityY, 0f, damping * Time.deltaTime);

        // Prevent overflow
        if (rotationX > 360f) rotationX -= 360f;
        if (rotationX < -360f) rotationX += 360f;

        transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
    }
}