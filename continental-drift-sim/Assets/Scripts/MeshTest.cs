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

    private void Awake()
    {
        mf = gameObject.AddComponent<MeshFilter>();
        mr = gameObject.AddComponent<MeshRenderer>();

        testCrust = new Crust(meshWidth, meshHeight, triWidth, triHeight, mf, mr);
        testPlate = new Plate();
        testPlate.Crust = testCrust;
    }

    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown("c"))
        {
            testCrust.BuildMesh();
        }

        if (Input.GetKeyDown("space"))
        {
        	Vector2 bottom_left = new Vector2(20, 20);
	        Vector2 top_left = new Vector2(20, 50);
	        Vector2 top_right = new Vector2(50, 70);
	        Vector2 bottom_right = new Vector2(40, 10);

	        testPlate.Outline = new Vector2[] { bottom_left, top_left, top_right, bottom_right };
            testPlate.DefaultHeight = 2.0f;
            testPlate.XSpeed = 1.0f;
            testPlate.ZSpeed = 3.0f;

            testPlate.DrawPlate();
        }

        if (Input.GetKeyDown("v"))
        {
            testPlate.MovePlate();
        }
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