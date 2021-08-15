using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraScript : MonoBehaviour
{
    public static CameraScript cScript;
    [HideInInspector]
    public UniversalAdditionalCameraData data;

    private void Awake()
    {
        if (cScript != null) { Destroy(cScript.gameObject); }
        data = GetComponent<UniversalAdditionalCameraData>();
        cScript = this;
    }
}
