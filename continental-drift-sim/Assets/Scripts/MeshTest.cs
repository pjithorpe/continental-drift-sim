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

        testCrust = new Crust(mf, mr, meshWidth, meshHeight, triWidth, triHeight);
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
	        Vector2 top_left = new Vector2(8, 50);
	        Vector2 top_right = new Vector2(60, 80);
	        Vector2 bottom_right = new Vector2(50, 20);
            Vector2 next = new Vector2(80, 20);
            Vector2 next2 = new Vector2(75, 30);
            Vector2 next2p5 = new Vector2(85, 90);
            Vector2 next3 = new Vector2(90, 30);
            Vector2 next4 = new Vector2(85, 5);
            Vector2 next5 = new Vector2(40, 5);

            testPlate = new Plate(outline: new Vector2[] { bottom_left, top_left, top_right, bottom_right, next, next2, next2p5, next3, next4, next5 });
            testPlate.Crust = testCrust;
            testPlate.DefaultHeight = 3.0f;
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