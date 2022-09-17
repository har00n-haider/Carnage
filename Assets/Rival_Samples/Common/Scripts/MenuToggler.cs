using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuToggler : MonoBehaviour
{
    public GameObject ToggledObject;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ToggledObject.SetActive(!ToggledObject.activeSelf);
        }
    }
}
