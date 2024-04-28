using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
/*
 * Generates a mesh with a vertex for each pixel of the provided image
 */
public class MeshGeneration : MonoBehaviour
{
    public Texture2D image;
    public Texture2D depth;
    public float[] pos;
    public int densityMultiplier = 1;

    protected Renderer meshRenderer;
    protected Mesh mesh;

    public void Awake()
    {
        meshRenderer = GetComponent<Renderer>();
    }

    public virtual void Setup()
    {
        int width = image.width * densityMultiplier;
        int height = image.height * densityMultiplier;
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = new Vector3[width * height];
        Vector3[] vertices = mesh.vertices;
        int[] indices = new int[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            indices[i] = i;
        }
        mesh.SetIndices(indices, MeshTopology.Points, 0);

        Vector2[] uvs = new Vector2[width * height];

        for (int i = 0; i < uvs.Length; i++)
        {
            float x = i % width / ((float)width);
            float y = i / width / ((float)height);
            uvs[i] = new Vector2(x, y);
        }
        mesh.uv = uvs;


        Bounds bounds = mesh.bounds;
        // Adjust the size of the bounds here. For example:
        bounds.extents = new Vector3(10, 10, 10);
        mesh.bounds = bounds;

        GetComponent<MeshFilter>().mesh = mesh;
        meshRenderer.material.SetTexture("_MainTex", image);
        meshRenderer.material.SetTexture("_Depth", depth);
    }

}
