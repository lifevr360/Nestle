using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject nestleMapObject;
    public VideoManager videoManager;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OnMapButtonClick()
    {
        if (nestleMapObject != null)
        {
            bool newState = !nestleMapObject.activeSelf;
            nestleMapObject.SetActive(newState);

            if (videoManager != null)
            {
                if (newState)
                {
                    // Map opened → Pause video
                    videoManager.PauseVideo();
                }
                else
                {
                    // Map closed → Resume video
                    videoManager.ResumeVideo();
                }
            }
        }
    }

    public void OnHotspotClick(GameObject obj)
    {
        if(obj!=null)
        {
            obj.SetActive(!obj.activeSelf);
        }
    }
}
