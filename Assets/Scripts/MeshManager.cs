using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using cdep;

public class MeshManager : MonoBehaviour
{
    public bool cdep = true;
    public Camera cam;
    public bool drivePosFromCam = true;
    public Vector3 cdepCameraPosition = Vector3.zero;
    public Vector3 cdepCameraDirection = Vector3.zero;
    public int maxMeshes = 8;
    public Texture2D[] images;
    public Texture2D[] depths;
    public Vector3[] positions;
    public String depthName;
    public int densityMultiplier = 1;
    public GameObject meshTemplate;
    //  public MeshGeneration[] meshes;
    private float aspect = 1;
    private List<Capture> captures = new List<Capture>();

    void Start()
    {
        aspect = Camera.main.aspect;
        if (!(images.Length == depths.Length && depths.Length == positions.Length))
        {
            Debug.LogError("expected parrallel arrays but one length differed");
        }
        MeshGeneration meshGenScript;
        for (int i = 0; i < images.Length && i < maxMeshes; i++)
        {
            InitializeOdsTextures(Application.streamingAssetsPath + "/" + depthName, i);
            Texture2D image = images[i];
            Texture2D depth = depths[i];
            
            if (cdep)
            {
                meshGenScript = Instantiate(meshTemplate, new Vector3(0,0,0), Quaternion.Euler(-180, 0, 0)).GetComponent<MeshGeneration>();
                float[] position = { positions[i].x, positions[i].y, positions[i].z};
                meshGenScript.pos = position;
            }
            else {
                float[] position = { positions[i].z, positions[i].y, -positions[i].x };
                meshGenScript = Instantiate(
                    meshTemplate, new Vector3(position[0], position[1], position[2]), Quaternion.Euler(-90, 0, 0)
                ).GetComponent<MeshGeneration>();
            }
            meshGenScript.depth = depth;
            meshGenScript.image = image;
            meshGenScript.Setup();
            captures.Add(new Capture() {image = image, depth = depth, position = positions[i], meshGenScript = meshGenScript});
        }
    }

    public void Update()
    {
        if (cdep) {
            if (drivePosFromCam)
            {
                cdepCameraPosition = new Vector3(Camera.main.transform.position.z, Camera.main.transform.position.y, Camera.main.transform.position.x);

                float cameraPitch = -Camera.main.transform.rotation.eulerAngles.x;
                float cameraYaw = -Camera.main.transform.rotation.eulerAngles.y;

                // Create rotation quaternions
                Quaternion rotationX = Quaternion.Euler(cameraPitch, 0, 0);
                Quaternion rotationY = Quaternion.Euler(0, cameraYaw - 90, 0);

                // Create direction vector
                Vector3 direction = new Vector3(0, 0, -1);

                // Apply transformations
                cdepCameraDirection = rotationY * rotationX * direction;
            }
            captures = captures.OrderBy(x => Vector3.Distance(x.position, cdepCameraPosition)).ToList();
            for (int i = 0; i < captures.Count; i++)
            {
                CDEPMeshGeneration meshGen = ((CDEPMeshGeneration)captures[i].meshGenScript);
                if (i < maxMeshes)
                {
                    if(aspect != Camera.main.aspect)
                    {
                        meshGen.SetAspect(Camera.main.aspect);
                    }
                    meshGen.gameObject.SetActive(true);
                    meshGen.SetCamPos(cdepCameraPosition - captures[i].position);
                    meshGen.SetCamDirection(cdepCameraDirection);
                    meshGen.SetCameraIndex(i);
                }
                else
                {
                    meshGen.gameObject.SetActive(false);
                }
            }
            aspect = Camera.main.aspect;
        }
    }

    void InitializeOdsTextures(string file_name, int index)
    {
        // Load from file path and save as texture - color
        string textureImagePath = file_name + "_" + (index+1) + ".png";
        byte[] bytes = File.ReadAllBytes(textureImagePath);
        Texture2D loadTexture = new Texture2D(1, 1); //mock size 1x1
        loadTexture.LoadImage(bytes);
        images[index] = loadTexture;

        // Load from file path to texture asset - depth
        string depthImagePath = file_name + "_" + (index + 1) + ".depth";

        byte[] depthBytes = File.ReadAllBytes(depthImagePath);
        // Ensure the byte array length is a multiple of 4 (size of a float)
        if (depthBytes.Length % 4 != 0)
        {
            throw new ArgumentException("Byte array length must be a multiple of 4");
        }

        // Initialize float array
        float[] floatArray = new float[depthBytes.Length / 4];

        // Convert bytes to floats
        for (int i = 0; i < depthBytes.Length; i += 4)
        {
            floatArray[i / 4] = BitConverter.ToSingle(depthBytes, i);
        }

        Color[] colors = new Color[loadTexture.width * loadTexture.height];
        for (int i = 0; i < floatArray.Length; i++)
        {
            float val = floatArray[i];
            colors[floatArray.Length - i - 1] = new Color(val, val, val);
        }

        Texture2D depthLoadTexture = new Texture2D(loadTexture.width, loadTexture.height, TextureFormat.RFloat, false); //mock size 1x1
        depthLoadTexture.SetPixels(colors);
        depthLoadTexture.Apply();
        depths[index] = depthLoadTexture;
    }

}

