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

    [SerializeField] public int randomSeed = 1;

    Mesh mesh;
    MeshFilter mf;
    MeshRenderer mr;

    Crust testCrust;
    Plate testPlate;
    float animationTime;

    private void Awake()
    {
        Random.InitState(randomSeed);
        mf = gameObject.AddComponent<MeshFilter>();
        mr = gameObject.AddComponent<MeshRenderer>();

        testCrust = new Crust(mf, mr, meshWidth, meshHeight, triWidth, triHeight, baseHeight: 10.0f);
    }

    void Start()
    {
        animationTime = 5;
    }

    void Update()
    {
        if (Input.GetKeyDown("c"))
        {
            Debug.Log(testCrust.Stage.GetType().ToString());
            testCrust.BuildMesh(addNoise:true);
        }

        if (Input.GetKeyDown("space"))
        {
            
            animationTime -= Time.deltaTime;

            if (testCrust.Stage is CoolingStage)
            {
                testCrust.Stage = new WaterStage();
            }
            else if (testCrust.Stage is WaterStage)
            {
                testCrust.Stage = new LifeStage();
            }
            else if (testCrust.Stage is LifeStage)
            {
                testCrust.Stage = new CoolingStage();
            }

            testCrust.UpdateMesh(updateAll: true);
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