using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GeographyHelper;
using geo = GeographyHelper;

public class MeshTest : MonoBehaviour {

    [SerializeField] public float triWidth = 1f;
    [SerializeField] public float triHeight = 1f;

    [SerializeField] int meshWidth = 256;
    [SerializeField] public int meshHeight = 256;

    int vertexCount;
    int triCount;
    Vector3[] verts;
    int[] tris;
    Mesh mesh;
    MeshFilter mf;
    MeshRenderer mr;

    Plate testPlate;
    int[,] outlinePlot;

    private void Awake()
    {
        vertexCount = meshWidth * meshHeight;
        triCount = (meshWidth - 1) * (meshHeight -1) * 6;
        verts = new Vector3[vertexCount];
        tris = new int[triCount];

        mf = gameObject.AddComponent<MeshFilter>();
        mr = gameObject.AddComponent<MeshRenderer>();

        testPlate = new Plate();
    }

    void Start()
    {
        Debug.Log("CREATING MESH");

        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

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
                    tris[ti] = tris[ti + 3] = vi;
                    tris[ti + 1] = vi + meshWidth;
                    tris[ti + 2] = tris[ti + 4] = vi + meshWidth + 1;
                    tris[ti + 5] = vi + 1;
                }
            }
        }
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        
        mf.mesh = mesh;
        
        mr.material = Resources.Load("Materials/TestMaterial", typeof(Material)) as Material;

        Camera mainCam = Camera.main;
        mainCam.enabled = true;
        mainCam.aspect = 1;
        mainCam.transform.position = new Vector3(meshWidth * triWidth * 0.5f, 1.0f, meshHeight * triHeight * 0.5f);
        //This enables the orthographic mode
        mainCam.orthographic = true;
        //Set the size of the viewing volume you'd like the orthographic Camera to pick up (5)
        mainCam.orthographicSize = meshHeight * triWidth * 0.5f;
        //Set the orthographic Camera Viewport size and position
        mainCam.rect = new Rect(0.0f, 0.0f, meshWidth * triWidth, meshHeight * triHeight);

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

    void Update()
    {
    	

        if (Input.GetKeyDown("space"))
        {
        	Vector2 bottom_left = new Vector2(20, 20);
	        Vector2 top_left = new Vector2(20, 50);
	        Vector2 top_right = new Vector2(50, 70);
	        Vector2 bottom_right = new Vector2(40, 10);

	        testPlate.Outline = new Vector2[] { bottom_left, top_left, top_right, bottom_right };
            testPlate.DefaultHeight = 2.0f;
            Debug.Log("About to call GetVertexPlot()...");
            outlinePlot = testPlate.GetVertexPlot();
            var heights = new float[outlinePlot.GetLength(0)];

            //temp
            for (int i=0; i<heights.Length; i++)
            {
                heights[i] = testPlate.DefaultHeight;
            }

            UpdateMesh(outlinePlot, heights);

            Vector2[] oLine = testPlate.Outline;
        	if(oLine == null){
        		Debug.Log("broken");
        	}
        }

        /* test moving a plate
        if (Input.GetKeyDown("c"))
        {
        	var heights = new float[outlinePlot.GetLength(0)];
        	for (int i=0; i<heights.Length; i++)
            {
                heights[i] = 0.0f;
            }

        	UpdateMesh(outlinePlot, heights);
        	testPlate.SetXSpeed(1.0f);
        	testPlate.SetZSpeed(2.0f);

        	Vector2[] oLine = testPlate.GetOutline();
        	if(oLine == null){
        		Debug.Log("broken");
        	}
        	for (int i=0; i<oLine.Length; i++)
        	{
        		oLine[i].x += 1.0f;
        		oLine[i].y += 2.0f;
        	}

        	testPlate.SetOutline(oLine);
        	outlinePlot = testPlate.GetVertexPlot();

            //temp
            for (int i=0; i<heights.Length; i++)
            {
                heights[i] = testPlate.GetDefaultHeight();
            }

        	UpdateMesh(outlinePlot, heights);
        }
        */
    }

    //takes a set of x,y coords and the heights to change them to
    void UpdateMesh(int[,] changes, float[] heights)
    {
        if (changes.GetLength(0) != heights.Length)
        {
            //Error!
            Debug.Log("changes and heights arrays are different sizes!");
        }

        for (int i=0; i<changes.GetLength(0); i++)
        {
            Debug.Log("i: " + i.ToString() + " , xPos: " + changes[i, 0] + " , yPos: " + changes[i, 1]);
            int xPos = changes[i,0];
            int yPos = changes[i,1];
            int vertIndex = yPos * meshWidth + xPos;

            float h = heights[i];

            verts[vertIndex] = new Vector3(verts[vertIndex].x, h, verts[vertIndex].z);
        }

        mesh.vertices = verts;
        mf.mesh = mesh;
    }

    /*//USE FOR VERTEX VISUALISATION
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