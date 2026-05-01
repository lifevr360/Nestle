using UnityEngine;

public class UIFaceCamera : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // Make UI face camera
        transform.forward = cam.transform.forward;
    }
}