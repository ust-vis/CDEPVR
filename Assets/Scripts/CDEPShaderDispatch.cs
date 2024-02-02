using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using cdep;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;

public class CDEPShaderDispatch : MonoBehaviour
{
    public RenderTexture rtColor, rtDepth;
    public ComputeShader clearShader;
    private int clearShaderKernelID;
    public ComputeShader cdepShader;
    private int cdepKernelID;
    public ComputeShader textureGenShader;
    private int textureGenKernelID;
    public int threadGroupSize = 8;
    public int imagesToLoad = 8;
    public int imagesToRender = 8;
    public String depthName;
    public Vector3[] positions;
    public Vector3 camPos;
    public Vector2 resolution;

    private ComputeBuffer intermediateStorage;
    private int x, y;
    private List<Capture> captures;

    public bool drivePosFromHead;
    public Transform head;

    public bool renderLeft;
    public bool renderRight;
    public bool cullingEnabled;

    public GameObject[] textureObjects;
    public GameObject[] depthObjects;

    void Start()
    {
        x = (int)resolution.x;
        y = (int)resolution.y * 2;
        rtColor = new RenderTexture(x, y, 24);
        intermediateStorage = new ComputeBuffer(x * y, sizeof(uint));
        rtColor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
        rtColor.enableRandomWrite = true;
        rtColor.Create();

        rtDepth = new RenderTexture(x, y, 24);
        intermediateStorage = new ComputeBuffer(x * y, sizeof(uint));
        rtDepth.format = RenderTextureFormat.RFloat;
        rtDepth.enableRandomWrite = true;
        rtDepth.Create();

        foreach (GameObject go in textureObjects)
        {
            go.GetComponent<Renderer>().material.mainTexture = rtColor;
        }
        foreach (GameObject go in textureObjects)
        {
            go.GetComponent<Renderer>().material.SetTexture("_Depth", rtDepth);
        }

        // Find the kernel IDs
        clearShaderKernelID = clearShader.FindKernel("CLEAR");
        cdepKernelID = cdepShader.FindKernel("CDEP");
        textureGenKernelID = textureGenShader.FindKernel("RENDERTEXTURE");


        clearShader.SetBuffer(clearShaderKernelID, "out_rgbd", intermediateStorage);
        clearShader.SetInts("dims", x, y);

        textureGenShader.SetBuffer(textureGenKernelID, "_Rgbd", intermediateStorage);
        textureGenShader.SetTexture(textureGenKernelID, "_OutRgba", rtColor);
        textureGenShader.SetTexture(textureGenKernelID, "_OutDepth", rtDepth);
        textureGenShader.SetInts("dims", x, y);
        textureGenShader.SetFloat("z_max", 1);

        cdepShader.SetFloat("camera_ipd", 0.065f);
        cdepShader.SetFloat("camera_focal_dist", 1f);
        cdepShader.SetFloat("z_max", 10f);
        cdepShader.SetFloat("depth_hint", 1f);
        cdepShader.SetInt("use_xr", cullingEnabled ? 1 : 0);
        cdepShader.SetBool("renderLeftEye", renderLeft);
        cdepShader.SetBool("renderRightEye", renderRight);
        cdepShader.SetFloat("xr_aspect", Camera.main.aspect);
        cdepShader.SetFloat("xr_fovy", Camera.main.fieldOfView * 0.89f);
        //cdepShader.SetFloat("xr_fovy", 2 * Mathf.Atan(Mathf.Tan(Camera.main.fieldOfView / 2) * Camera.main.aspect));

        //cdepResources.PrintJson(Application.streamingAssetsPath + "/" + depthName, positions, imagesToLoad);
        //captures = cdepResources.InitializeOdsTextures(Application.streamingAssetsPath + "/" + depthName, positions, imagesToLoad).ToList();
        captures = cdepResources.InitializeOdsTextures(Application.streamingAssetsPath + "/room capture").ToList();

        if (captures.Count > 0)
        {
            cdepShader.SetInt("xres", (int)captures[0].image.width);
            cdepShader.SetInt("yres", (int)captures[0].image.height);
        }

        cdepShader.SetBuffer(cdepKernelID, "out_rgbd", intermediateStorage);

        //Render the buffer to the render texture    
        textureGenShader.Dispatch(textureGenKernelID, x / threadGroupSize, y / threadGroupSize, 1);
    }

    public void Update()
    {
        clearShader.Dispatch(clearShaderKernelID, x / threadGroupSize, y / threadGroupSize, 1);
        if (drivePosFromHead)
        {
            camPos = head.position;
        }
        //so unity cam correctly maps to new space
        Vector3 cdepCameraPosition = new Vector3(camPos.z, -camPos.y, camPos.x);
        captures = captures.OrderBy(x => Vector3.Distance(x.position, cdepCameraPosition)).ToList();


        float cameraPitch = Camera.main.transform.rotation.eulerAngles.x;
        float cameraYaw = -Camera.main.transform.rotation.eulerAngles.y;

        // Create rotation quaternions
        Quaternion rotationX = Quaternion.Euler(cameraPitch, 0, 0);
        Quaternion rotationY = Quaternion.Euler(0, cameraYaw - 90, 0);

        // Create direction vector
        Vector3 direction = new Vector3(0, 0, -1);

        // Apply transformations
        Vector3 cdepCameraDirection = rotationY * rotationX * direction;

        for (int i = 0; i < Math.Min(imagesToRender, captures.Count); i++)
        {
            cdepShader.SetVector("camera_position", cdepCameraPosition - captures[i].position);
            cdepShader.SetVector("xr_view_dir", cdepCameraDirection);
            cdepShader.SetTexture(cdepKernelID, "image", captures[i].image);
            cdepShader.SetTexture(cdepKernelID, "depths", captures[i].depth);
            cdepShader.SetFloat("depth_hint", -0.015f * i);

            // Dispatch the shader
            cdepShader.Dispatch(cdepKernelID, x / threadGroupSize, y / threadGroupSize, 1);

            //Render the buffer to the render texture
            textureGenShader.Dispatch(textureGenKernelID, x / threadGroupSize, y / threadGroupSize, 1);
        }
    }

    void OnDestroy()
    {
        // Release the compute buffer
        intermediateStorage.Release();
    }
}