using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class FollowCamera : MonoBehaviour
{
    public float distance = 0.45f;       // The desired distance from the camera
    public float verticalOffset = -0.25f; // The desired vertical offset from the camera

    public Transform mainCamera;        // Reference to the main camera

    public float constantXRotation = -16.95f; // The desired Rotation for X

    // Start is called before the first frame update
    void Start()
    {
        // Ensure we have a valid reference to the camera
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found in the scene. Make sure there is a camera tagged as 'MainCamera'.");
            enabled = false; // Disable the script to prevent errors
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Ensure we have a valid reference to the camera
        if (mainCamera != null)
        {
            // Calculate the desired position for the object
            Vector3 targetPosition = mainCamera.position + mainCamera.forward * distance;
            targetPosition.y = mainCamera.position.y + verticalOffset;

            // Move the object to the desired position
            transform.position = targetPosition;

            // Get the position of the camera
            Vector3 cameraPosition = mainCamera.position;

            // Set the desired X rotation and +180 for Y
            Quaternion rotation = Quaternion.Euler(constantXRotation, 180, 0);

            // Make the object look at the camera with the constant X rotation
            transform.LookAt(cameraPosition);
            transform.rotation *= rotation;
        }
    }
}

