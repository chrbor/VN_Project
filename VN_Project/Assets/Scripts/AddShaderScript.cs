using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddShaderScript : MonoBehaviour
{
    public Material material;

    [ExecuteAlways]
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(src, dest, material);
    }
}
