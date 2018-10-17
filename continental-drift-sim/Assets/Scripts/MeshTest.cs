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

    Mesh mesh;
    MeshFilter mf;
    MeshRenderer mr;

    Crust testCrust;
    Plate testPlate;
    int[,] outlinePlot;

    private void Awake()
    {
        mf = gameObject.AddComponent<MeshFilter>();
        mr = gameObject.AddComponent<MeshRenderer>();

        testCrust = new Crust(meshWidth, meshHeight, mf, mr);
        testCrust.BuildMesh();
        testPlate = new Plate();
        testPlate.Crust = testCrust;
    }

    void Start()
    {
        
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
        	
        }
        */
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