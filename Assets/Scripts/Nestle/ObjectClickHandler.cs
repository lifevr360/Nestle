using UnityEngine;

public class ObjectClickHandler : MonoBehaviour
{
    public VideoManager videoManager;
    public int index;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("click is detected");

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);

            foreach (RaycastHit hit in hits)
            {
                Debug.Log("Hit: " + hit.collider.gameObject.name);

                if (hit.collider.transform.IsChildOf(transform))
                {
                    Debug.Log("Object Clicked: " + this.gameObject.name);
                    videoManager.PlayVideo(index);
                    break;
                }
            }
        }
    }
}