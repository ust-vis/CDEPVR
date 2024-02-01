using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ComputeShaderDispatch : MonoBehaviour
{
    public ComputeShader shader;
    public RenderTexture rt;
    public GameObject plane;
    public Texture2D[] textures;
    // Start is called before the first frame update
    void Start()
    {
        rt = new RenderTexture(1024,1024,24);
        rt.enableRandomWrite = true;
        rt.Create();
        plane.GetComponent<Renderer>().material.mainTexture = rt;  
        shader.SetTexture(0, "Result", rt);
        shader.Dispatch(0, rt.width, rt.height, 1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
