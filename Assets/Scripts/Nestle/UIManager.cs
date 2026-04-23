using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject nestleMapObject;
    // Start is called before the first frame update
    void Start()
    {
        
    }

   public void OnMapButtonClick()
    {
        if (nestleMapObject != null)
        {
            nestleMapObject.SetActive(!nestleMapObject.activeSelf);
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
