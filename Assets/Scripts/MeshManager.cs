using cdep;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * The point cloud and traditional pipeline approach are made up of multiple meshes placed in space.
 * Each mesh has different color and depth data. This script manages those meshes.
 * 
 * This script is a mess as it was mostly experimentation trying to get the algorithm to work ontop of 
 * the same script that implemented the point cloud logic. 
 * The CDEPShaderDispatch rewrites a lot of this logic in a nicer way. 
 */
public class MeshManager : MonoBehaviour
{
    public bool cdep = true;
    public Camera cam;
    public bool drivePosFromCam = true;
    public Vector3 cdepCameraPosition = Vector3.zero;
    public Vector3 cdepCameraDirection = Vector3.zero;
    public int maxMeshes = 8;
    public int imagesToLoad = 8;
    public String depthName;
    public int densityMultiplier = 1;
    //This is 
    public GameObject meshTemplate;
    //  public MeshGeneration[] meshes;
    private float aspect = 1;
    private List<Capture> captures = new List<Capture>();

    void Start()
    {
        aspect = Camera.main.aspect;
        MeshGeneration meshGenScript;
        captures = CdepResources.InitializeOdsTextures(Application.streamingAssetsPath + "/room capture", imagesToLoad).ToList();
        for (int i = 0; i < captures.Count && i < imagesToLoad; i++)
        {
            Texture2D image = captures[i].image;
            Texture2D depth = captures[i].depth;
            captures[i].position.y *= -1;
            Vector3 pos = captures[i].position;

            if (cdep)
            {
                meshGenScript = Instantiate(meshTemplate, new Vector3(0, 0, 0), Quaternion.Euler(-180, 0, 0)).GetComponent<MeshGeneration>();
                float[] position = { pos.x, pos.y, pos.z};
                meshGenScript.pos = position;
            }
            else
            {
                float[] position = { pos.z, pos.y, -pos.x };
                meshGenScript = Instantiate(
                    meshTemplate, new Vector3(position[0], position[1], position[2]), Quaternion.Euler(-90, 0, 0)
                ).GetComponent<MeshGeneration>();
            }
            meshGenScript.depth = depth;
            meshGenScript.image = image;
            meshGenScript.Setup();
            captures[i].meshGenScript = meshGenScript;
        }
    }

    public void Update()
    {
        //If CDEP is false we are rendering a point cloud, no updates are needed
        if (!cdep) return;

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
                if (aspect != Camera.main.aspect)
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

