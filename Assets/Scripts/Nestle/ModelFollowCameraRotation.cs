using UnityEngine;

public class ModelFollowCamera : MonoBehaviour
{
    public Transform targetCamera;

    private Vector3 positionOffset;
    private Quaternion rotationOffset;

    private bool wasInteracting = false;

    void Start()
    {
        if (targetCamera != null)
        {
            positionOffset = targetCamera.InverseTransformPoint(transform.position);
            rotationOffset = Quaternion.Inverse(targetCamera.rotation) * transform.rotation;
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // While interacting, update offset dynamically
        if (LookAround.isModelInteracting)
        {
            positionOffset = targetCamera.InverseTransformPoint(transform.position);
            rotationOffset = Quaternion.Inverse(targetCamera.rotation) * transform.rotation;
            wasInteracting = true;
            return;
        }

        if (wasInteracting)
        {
            wasInteracting = false;
        }

        // Follow camera like a child
        transform.position = targetCamera.TransformPoint(positionOffset);
        transform.rotation = targetCamera.rotation * rotationOffset;
    }
}