using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshTest : MonoBehaviour {

    float triWidth = 1f;
    float triHeight = 1f;

    int meshWidth = 10;
    int meshHeight = 10;

    void Start()
    {
        Debug.Log("CREATING MESH");
        MeshFilter mf = GetComponent<MeshFilter>();

        var mesh = new Mesh();
        mf.mesh = mesh;

        int vertexCount = meshWidth * meshHeight;

        Vector3[] vertices = new Vector3[vertexCount];

        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(triWidth, 0, 0);
        vertices[2] = new Vector3(0, triHeight, 0);
        vertices[3] = new Vector3(triWidth, triHeight, 0);

        mesh.vertices = vertices;

        int[] tri = new int[6];

        for (int i=0; i<vertexCount; i++)
        {

        }

        mesh.triangles = tri;

        Vector3[] normals = new Vector3[4];

        normals[0] = -Vector3.forward;
        normals[1] = -Vector3.forward;
        normals[2] = -Vector3.forward;
        normals[3] = -Vector3.forward;

        mesh.normals = normals;

        Vector2[] uv = new Vector2[4];

        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);

        mesh.uv = uv;
    }
}
