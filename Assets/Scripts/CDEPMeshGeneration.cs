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

    public void SetCamEye(float eye)
    {
        renderer.material.SetFloat("_camera_eye", eye);
    }
     
    public void SetCamPos(Vector3 cameraPos)
    {
        renderer.material.SetVector("_camera_position", cameraPos);
    }

    public void SetCamDirection(Vector4 cameraDir)
    {
        renderer.material.SetVector("_xr_view_dir", cameraDir);
    }

    public void SetAspect(float aspectRatio)
    {
        renderer.material.SetFloat("_xr_aspect", aspectRatio);
    }

    public void SetCameraIndex(int idx)
    {
        renderer.material.SetFloat("_img_index", idx);
    }
}
