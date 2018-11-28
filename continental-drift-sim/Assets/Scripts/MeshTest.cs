using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    float moveSpeed;

    bool cooling;
    bool move;

    private void Awake()
    {
        Random.InitState(randomSeed);
        mf = gameObject.AddComponent<MeshFilter>();
        mr = gameObject.AddComponent<MeshRenderer>();

        testCrust = new Crust(mf, mr, meshWidth, meshHeight, triWidth, triHeight, baseHeight: 10.0f, maxHeight: 20.0f, seaLevel: 1.0f);
    }

    void Start()
    {
        moveSpeed = 1.0f;
        coolingTime = 4;
        testCrust.Stage = new WaterStage();
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
            Debug.Log("you pressed c.");
            testCrust.BuildMesh(addNoise:true);
            Debug.Log("we built this mesh on rosk and roll");
            testCrust.InitialiseCrust(10);
            Debug.Log("crust init boi");

            for (int i = 0; i < testCrust.Plates.Length; i++)
            {
                testCrust.Plates[i].XSpeed = Random.Range(-2, 3);
                testCrust.Plates[i].ZSpeed = Random.Range(-2, 3);
                Debug.Log("you have a plate, and");
            }
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

        if (Input.GetKeyDown("b"))
        {
            testCrust.UpdateMesh();
        }

        if (Input.GetKeyDown("v"))
        {
            InvokeRepeating("UpdateTestMesh", 0.0f, moveSpeed);
        }
    }

    void UpdateTestMesh()
    {
        Debug.Log(testCrust.Stage.GetType().ToString());
        testCrust.UpdateMesh();
    }

    private void OnDrawGizmos()
    {
        /*
        Gizmos.color = Color.red;
        if (m_points != null)
        {
            for (int i = 0; i < m_points.Count; i++)
            {
                Gizmos.DrawSphere(m_points[i], 0.2f);
            }
        }

        if (m_edges != null)
        {
            Gizmos.color = Color.white;
            for (int i = 0; i < m_edges.Count; i++)
            {
                for (int j = 1; j < m_edges[i].Count; j++)
                {
                    Vector3 left = new Vector3(m_edges[i][j - 1].x, 100.0f, m_edges[i][j - 1].y);
                    Vector3 right = new Vector3(m_edges[i][j].x, 100.0f, m_edges[i][j].y);
                    Gizmos.DrawLine(left, right);
                }
                Vector3 l = new Vector3(m_edges[i][m_edges[i].Count - 1].x, 100.0f, m_edges[i][m_edges[i].Count - 1].y);
                Vector3 r = new Vector3(m_edges[i][0].x, 100.0f, m_edges[i][0].y);
                Gizmos.DrawLine((Vector3)l, (Vector3)r);
            }
        }
        */
    }

}