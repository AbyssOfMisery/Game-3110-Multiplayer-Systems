using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkCube : MonoBehaviour
{
    private string id;

    public void Setup(string _id)
    {
        id = _id;
    }

    // Changes Color Every Second - on server Update message
    public void ChangeColor(float r, float g, float b)
    {
        this.gameObject.GetComponent<Renderer>().material.color = 
            new Color(
                Random.Range(0.0f,1.0f),
                Random.Range(0.0f,1.0f),
                Random.Range(0.0f,1.0f),
                1.0f
            );        

    }
}
