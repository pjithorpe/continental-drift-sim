using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshTest : MonoBehaviour {

    float triWidth = 1f;
    float triHeight = 1f;

    int meshWidth = 225;
    int meshHeight = 225;

    int vertexCount;
    int triCount;
    Vector3[] verts;
    int[] tris;

    private void Awake()
    {
        vertexCount = meshWidth * meshHeight;
        triCount = (meshWidth - 1) * (meshHeight -1) * 6;
        verts = new Vector3[vertexCount];
        tris = new int[triCount];
    }

    void Start()
    {
        Debug.Log("CREATING MESH");

        var mesh = new Mesh();

        //x and y (in number of triWidths/Lengths)
        int xPos;
        int zPos;

        //vertices
        for (int i=0; i<verts.Length; i++)
        {
            
            xPos = i % meshWidth;
            zPos = i / meshWidth;

            if (zPos % 2 == 0)
            {
                verts[i] = new Vector3(xPos * triWidth, 0, zPos * triHeight);
            }
            else
            {
                verts[i] = new Vector3((xPos * triWidth) + (triWidth / 2), 0, zPos * triHeight);
            }

        }

        mesh.vertices = verts;


        //tris
        //vi = vertex index
        //ti = triangle index
        for (int ti=0, vi=0, y=0; y<meshHeight-1; y++, vi++)
        {
            for (int x = 0; x < meshWidth-1; x++, ti+=6, vi++)
            {
                if ((vi / meshWidth) % 2 == 0)
                {
                    tris[ti] = vi;
                    tris[ti + 3] = tris[ti + 2] = vi + 1;
                    tris[ti + 4] = tris[ti + 1] = vi + meshWidth;
                    tris[ti + 5] = vi + meshWidth + 1;
                }
                else
                {
                    tris[ti] = vi;
                    tris[ti + 1] = vi + meshWidth;
                    tris[ti + 2] = vi + meshWidth + 1;
                    tris[ti + 3] = vi;
                    tris[ti + 4] = vi + meshWidth + 1;
                    tris[ti + 5] = vi + 1;
                }
            }
        }
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = Resources.Load("Materials/TestMaterial", typeof(Material)) as Material;

        /*Vector3[] normals = new Vector3[tris.Length];

        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = Vector3.up;
        }

        mesh.normals = normals;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        */
        /*Vector2[] uv = new Vector2[vertexCount];

        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(0, 1);
        uv[3] = new Vector2(1, 1);

        mesh.uv = uv;*/
    }

    /* USE FOR VERTEX VISUALISATION
    private void OnDrawGizmos()
    {
        if (verts != null)
        {
            Gizmos.color = Color.black;
            for (int i = 0; i < verts.Length; i++)
            {
                Gizmos.DrawSphere(verts[i], 0.1f);
            }
        }
    }*/

}
