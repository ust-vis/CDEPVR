using cdep;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Management class for compute shader implementation
 */
public class CDEPShaderDispatch : MonoBehaviour
{
    // Render textures store references to buffers on the GPU we can pass between shaders without moving from VRAM to RAM
    // These textures are the final outputs of the CDEP shaders and are used for projection on the spheres
    public RenderTexture rtColor, rtDepth;
    [Header("Compute Shaders")]
    // One buffer is continuously written in multiple passes for each capture.
    // This Shader clears the buffer at the start of the frame
    public ComputeShader clearShader;
    private int clearShaderKernelID;
    // The main CDEP shader that generates the visuals
    public ComputeShader cdepShader;
    private int cdepKernelID;
    // This shader generates the render textures
    public ComputeShader textureGenShader;
    private int textureGenKernelID;
    // Handles interpolation of closest shaders
    public ComputeShader interpolationShader;
    private int interpolationKernelID;

    // Thread group size for dispatching. For complex reasons slightly beyond me this should probably be kept at 8
    public int threadGroupSize = 8;
    public int imagesToLoad = 8;
    public int imagesToRender = 8;
    public Vector2 resolution;

    // This first intermetiate storage is the buffer that is cleared and rewritten to for each CDEP pass.
    private ComputeBuffer intermediateStorage;
    // If interpolation is enabled data is written to the prior buffer on pass zero. Then subsequent passes
    // Are written to this second buffer and merged back into the first buffer
    private ComputeBuffer intermediateStorage2;
    private int x, y;
    private List<Capture> captures;
    private float ipd;

    [Header("Positioning")]
    public bool drivePosFromHead;
    public Vector3 camPos;
    public Transform head;

    public bool renderLeft;
    public bool renderRight;
    public bool cullingEnabled;
    [Header("Interpolation")]
    public bool InterpolationEnabled;
    public int InterpolationSteps = 2;
    // The Epsilon for considering two points to have the same position
    public float mergeDistance = 0.05f;


    public GameObject[] textureObjects;
    public GameObject[] depthObjects;

    private void OnEnable()
    {
        x = (int)resolution.x;
        y = (int)resolution.y * 2;
        rtColor = new RenderTexture(x, y, 24);
        rtDepth = new RenderTexture(x, y, 24);
        intermediateStorage = new ComputeBuffer(x * y, sizeof(uint));
        intermediateStorage2 = new ComputeBuffer(x * y, sizeof(uint));

        // Find the kernel IDs
        clearShaderKernelID = clearShader.FindKernel("CLEAR");
        cdepKernelID = cdepShader.FindKernel("CDEP");
        textureGenKernelID = textureGenShader.FindKernel("RENDERTEXTURE");
        interpolationKernelID = interpolationShader.FindKernel("INTERPOLATE");
    }

    void Start()
    {
        rtColor.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
        rtColor.enableRandomWrite = true;
        rtColor.Create();

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

        //pass all the necessary references to the shaders
        clearShader.SetBuffer(clearShaderKernelID, "out_rgbd", intermediateStorage);
        clearShader.SetInts("dims", x, y);

        textureGenShader.SetBuffer(textureGenKernelID, "_Rgbd", intermediateStorage);
        textureGenShader.SetTexture(textureGenKernelID, "_OutRgba", rtColor);
        textureGenShader.SetTexture(textureGenKernelID, "_OutDepth", rtDepth);
        textureGenShader.SetInts("dims", x, y);
        textureGenShader.SetFloat("z_max", 1);

        cdepShader.SetFloat("camera_focal_dist", 1f);
        cdepShader.SetFloat("z_max", 10f);
        cdepShader.SetFloat("depth_hint", 1f);
        cdepShader.SetInt("use_xr", cullingEnabled ? 1 : 0);
        cdepShader.SetBool("renderLeftEye", renderLeft);
        cdepShader.SetBool("renderRightEye", renderRight);
        cdepShader.SetFloat("xr_aspect", Camera.main.aspect);
        cdepShader.SetFloat("xr_fovy", Camera.main.fieldOfView * 0.89f);

        interpolationShader.SetBuffer(interpolationKernelID, "pass1", intermediateStorage);
        interpolationShader.SetBuffer(interpolationKernelID, "pass2", intermediateStorage2);

        captures = CdepResources.InitializeOdsTextures(Application.streamingAssetsPath + "/room capture", imagesToLoad).ToList();

        if (captures.Count > 0)
        {
            cdepShader.SetInt("xres", (int)captures[0].image.width);
            cdepShader.SetInt("yres", (int)captures[0].image.height);
            interpolationShader.SetInt("xres", (int)captures[0].image.width);
            interpolationShader.SetInt("yres", (int)captures[0].image.height * 2);
        }

        cdepShader.SetBuffer(cdepKernelID, "out_rgbd", intermediateStorage);

        //Render the buffer to the render texture    
        textureGenShader.Dispatch(textureGenKernelID, x / threadGroupSize, y / threadGroupSize, 1);

        // This sets the IPD to the actual ipd set by the user of the headeset
        ipd = Camera.main.stereoSeparation;
    }


    public void Update()
    {
        //prevent sensor noise constantly chainging the ipd
        if (MathF.Abs(ipd - Camera.main.stereoSeparation) > 0.0001)
        {
            ipd = Camera.main.stereoSeparation;
            cdepShader.SetFloat("camera_ipd", Camera.main.stereoSeparation * 2);
            Debug.Log("IPD changed: " + Camera.main.stereoSeparation);
        }

        //clear both of the buffers for the frame
        clearShader.SetBuffer(clearShaderKernelID, "out_rgbd", intermediateStorage);
        clearShader.Dispatch(clearShaderKernelID, x / threadGroupSize, y / threadGroupSize, 1);

        clearShader.SetBuffer(clearShaderKernelID, "out_rgbd", intermediateStorage2);
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
            // we want to render this to the secondary buffer then merge the primary and secondary buffers 
            // back into the primary buffer but only by the amount of interpolation steps as to not hurt performance too much
            if (InterpolationEnabled && i > 0 && i <= InterpolationSteps)
            {
                cdepShader.SetBuffer(cdepKernelID, "out_rgbd", intermediateStorage2);
                cdepShader.SetVector("camera_position", cdepCameraPosition - captures[i].position);
                cdepShader.SetVector("xr_view_dir", cdepCameraDirection);
                cdepShader.SetTexture(cdepKernelID, "image", captures[i].image);
                cdepShader.SetTexture(cdepKernelID, "depths", captures[i].depth);
                cdepShader.SetFloat("depth_hint", 0);
                cdepShader.Dispatch(cdepKernelID, x / threadGroupSize, y / threadGroupSize, 1);

                float dist1 = Vector3.Distance(cdepCameraPosition, captures[0].position);
                float dist2 = Vector3.Distance(cdepCameraPosition, captures[i].position);
                float dist = dist1 / (dist1 + dist2);

                interpolationShader.SetFloat("percentDistance", dist);
                interpolationShader.SetFloat("mergeDistance", mergeDistance);
                interpolationShader.Dispatch(interpolationKernelID, x / threadGroupSize, y / threadGroupSize, 1);
            }
            else
            {
                cdepShader.SetBuffer(cdepKernelID, "out_rgbd", intermediateStorage);
                cdepShader.SetVector("camera_position", cdepCameraPosition - captures[i].position);
                cdepShader.SetVector("xr_view_dir", cdepCameraDirection);
                cdepShader.SetTexture(cdepKernelID, "image", captures[i].image);
                cdepShader.SetTexture(cdepKernelID, "depths", captures[i].depth);
                cdepShader.SetFloat("depth_hint", 0.015f * i);
                cdepShader.Dispatch(cdepKernelID, x / threadGroupSize, y / threadGroupSize, 1);
            }
        }
        //Render the buffer to the render texture
        textureGenShader.Dispatch(textureGenKernelID, x / threadGroupSize, y / threadGroupSize, 1);
    }

    void OnDestroy()
    {
        // Release the compute buffer
        intermediateStorage.Release();
        intermediateStorage2.Release();
    }
}