using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class matInitScript : MonoBehaviour
{
    private void Awake()
    {
        Material mat = new Material(GetComponent<SpriteRenderer>().material);
        GetComponent<SpriteRenderer>().material = mat;
    }
}
