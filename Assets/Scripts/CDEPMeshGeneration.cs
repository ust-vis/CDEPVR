using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CDEPMeshGeneration : MeshGeneration
{
    // Start is called before the first frame update
    public override void Setup()
    {
        base.Setup();
    }
     
    public void SetCamPos(Vector3 cameraPos)
    {
        meshRenderer.material.SetVector("_camera_position", cameraPos);
    }

    public void SetCamDirection(Vector4 cameraDir)
    {
        meshRenderer.material.SetVector("_xr_view_dir", cameraDir);
    }

    public void SetAspect(float aspectRatio)
    {
        meshRenderer.material.SetFloat("_xr_aspect", aspectRatio);
    }

    public void SetCameraIndex(int idx)
    {
        meshRenderer.material.SetFloat("_img_index", idx);
    }
}
