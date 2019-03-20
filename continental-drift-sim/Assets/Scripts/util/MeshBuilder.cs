using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshBuilder
{
    // Start is called before the first frame update
    public static Mesh BuildMesh(MeshFilter mf, MeshRenderer mr, int width, int height, float triSize, bool addNoise)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        int vertexCount = width * height;
        int triCount = (width - 1) * (height - 1) * 6;
        float halfTriSize = triSize / 2f;
        Vector3[] verts = new Vector3[vertexCount];

        int[] tris = new int[triCount];

        //x and y (in number of triWidths/Lengths)
        int xPos;
        int zPos;

        //vertices
        for (int i = 0; i < verts.Length; i++)
        {
            xPos = i % width;
            zPos = i / width;

            if (zPos % 2 == 0)
            {
                verts[i] = new Vector3(xPos * triSize, 0f, zPos * triSize);
            }
            else
            {
                verts[i] = new Vector3((xPos * triSize) + halfTriSize, 0f, zPos * triSize);
            }
        }

        mesh.vertices = verts;


        //triangles
        //vi = vertex index
        //ti = triangle index
        for (int ti = 0, vi = 0, y = 0; y < height - 1; y++, vi++)
        {
            for (int x = 0; x < width - 1; x++, ti += 6, vi++)
            {
                if ((vi / width) % 2 == 0)
                {
                    tris[ti] = vi;
                    tris[ti + 3] = tris[ti + 2] = vi + 1;
                    tris[ti + 4] = tris[ti + 1] = vi + width;
                    tris[ti + 5] = vi + width + 1;
                }
                else
                {
                    tris[ti] = tris[ti + 3] = vi;
                    tris[ti + 1] = vi + width;
                    tris[ti + 2] = tris[ti + 4] = vi + width + 1;
                    tris[ti + 5] = vi + 1;
                }
            }
        }
        mesh.triangles = tris;


        var bedrockGrey = new Color32(79, 70, 60, 255);
        Color[] colors = new Color[verts.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = bedrockGrey;
        }

        mesh.colors = colors;
        mesh.RecalculateNormals();
        mf.mesh = mesh;
        mr.material = Resources.Load("Materials/TestMaterial", typeof(Material)) as Material;

        return mesh;
    }



    public static List<CrustNode>[,] BuildCrustNodesArray(int width, int height)
    {
        List<CrustNode>[,] nodes;

        nodes = new List<CrustNode>[width, height];

        for (int xPos = 0; xPos < width; xPos++)
        {
            for (int zPos = 0; zPos < height; zPos++)
            {
                CrustNode n = ObjectPooler.current.GetPooledNode();

                n.X = xPos;
                n.Z = zPos;
                n.Height = 0f;
                n.Density = 0.1f;
                n.IsVirtual = false;

                nodes[xPos, zPos] = new List<CrustNode>();
                nodes[xPos, zPos].Add(n);
            }
        }

        return nodes;
    }

    public static LinkedList<CrustNode>[,] BuildMovedCrustNodesArray(int width, int height)
    {
        LinkedList<CrustNode>[,] movedNodes;

        movedNodes = new LinkedList<CrustNode>[width, height];

        for (int xPos = 0; xPos < width; xPos++)
        {
            for (int zPos = 0; zPos < height; zPos++)
            {
                movedNodes[xPos, zPos] = new LinkedList<CrustNode>();
            }
        }

        return movedNodes;
    }
}
