using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTexCamScript : MonoBehaviour
{
    public static RTexCamScript rTexScript;
    public bool active = true;

    RenderTexture rTex, rTex2;
    public Material transition;
    public Material camMat;

    private Camera rTexCam;

    void Awake()
    {
        //Singleton:
        if(rTexScript != null) { Destroy(gameObject); active = false; return; }
        rTexScript = this;

        rTexCam = GetComponent<Camera>();

        //erstelle textur, die das Bild der Camera aufnimmt:
        rTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.DefaultHDR);
        rTex.Create();
        //erstelle textur, auf die der Übergangseffekt angewendet wird:
        rTex2 = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.DefaultHDR);
        rTex2.Create();
        rTexCam.targetTexture = rTex;

        camMat.SetTexture("_preTex", rTex2);
    }

    private void Update()
    {
        //wenn nicht aktiv, dann überspringe das Update
        if (!active) return;

        //setze texturen zurück auf null:
        RenderTexture rt = UnityEngine.RenderTexture.active;
        UnityEngine.RenderTexture.active = rTex;
        GL.Clear(true, true, Color.clear);//Camera.main.backgroundColor);
        UnityEngine.RenderTexture.active = rTex2;
        GL.Clear(true, true, Color.clear);//Camera.main.backgroundColor);
        UnityEngine.RenderTexture.active = rt;
        rTexCam.Render();

        //füge transition über Blit-operator hinzu:
        Graphics.Blit(rTex, rTex2, transition);
    }
}
