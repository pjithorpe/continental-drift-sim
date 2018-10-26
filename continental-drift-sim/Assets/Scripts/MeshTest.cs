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

    float t;
    float coolingTime;

    bool cooling;

    List<List<int[,]>> m_edges;

    private void Awake()
    {
        Random.InitState(randomSeed);
        mf = gameObject.AddComponent<MeshFilter>();
        mr = gameObject.AddComponent<MeshRenderer>();

        testCrust = new Crust(mf, mr, meshWidth, meshHeight, triWidth, triHeight, baseHeight: 10.0f, maxHeight: 20.0f, seaLevel: 1.0f);
    }

    void Start()
    {
        coolingTime = 4;
        testCrust.Stage = new WaterStage();
        testCrust.UpdateMesh(updateAll: true);
    }

    void Update()
    {
        t = Time.deltaTime/coolingTime;

        if (Input.GetKeyDown("x"))
        {
            cooling = true;
        }

        if (cooling)
        {
            testCrust.SeaLevel -= t;
            Debug.Log("Sea level is now: " + (testCrust.SeaLevel * 100).ToString() + "%");
            testCrust.UpdateMesh(updateAll: true);

            if(testCrust.SeaLevel <= -0.1f)
            {
                cooling = false;
            }
        }

        if (Input.GetKeyDown("c"))
        {
            Debug.Log(testCrust.Stage.GetType().ToString());
            testCrust.BuildMesh(addNoise:true);
            m_edges = testCrust.InitialiseCrust(10);

            
        }

        if (Input.GetKeyDown("z"))
        {
            testCrust.SeaLevel += 0.05f;
            Debug.Log("Sea level is now: " + (testCrust.SeaLevel * 100).ToString() + "%");
            testCrust.UpdateMesh(updateAll: true);
        }

        if (Input.GetKeyDown("space"))
        {
            
            

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

    private void OnDrawGizmos()
    {
        if (m_edges != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < m_edges[0].Count; i++)
            {
                for (int j=1; j< m_edges[0][i].GetLength(0); j++)
                {
                    Vector3 left = new Vector3(m_edges[0][i][j - 1, 0], 100.0f, m_edges[0][i][j - 1, 1]);
                    Vector3 right = new Vector3(m_edges[0][i][j, 0], 100.0f, m_edges[0][i][j, 1]);
                    Gizmos.DrawLine(left, right);
                }
                
            }

            Gizmos.color = Color.red;
            for (int i = 0; i < m_edges[1][0].GetLength(0); i++)
            {
                Vector3 right = new Vector3(m_edges[1][0][i, 0], 100.0f, m_edges[1][0][i, 1]);
                Gizmos.DrawSphere(right, 5.0f);
            }
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