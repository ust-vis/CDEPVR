using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class testMeshScript : MonoBehaviour
{
    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int[] indices = new int[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            indices[i] = i;
        }

        Mesh pointMesh = new Mesh();
        pointMesh.vertices = vertices;
        pointMesh.SetIndices(indices, MeshTopology.Points, 0);

        GetComponent<MeshFilter>().mesh = pointMesh;
    }
}