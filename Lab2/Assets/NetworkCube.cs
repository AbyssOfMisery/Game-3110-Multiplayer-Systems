using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkCube : MonoBehaviour
{
    public float angularSpeed = 200;
    public string id = string.Empty;
    // Start is called before the first frame update
    void Start()
    {
    }

    public void Setup(string _id)
    {
        id = _id;
    }

    // Update is called once per frame
    void Update()
    {
        var angle = angularSpeed * Time.deltaTime;
        this.transform.Rotate(0,angle,0);
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
