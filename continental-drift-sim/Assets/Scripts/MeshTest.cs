using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MeshTest : MonoBehaviour {

    [SerializeField] public float triWidth = 1f;
    [SerializeField] public float triHeight = 1f;

    [SerializeField] int meshWidth = 256;
    [SerializeField] public int meshHeight = 256;

    [SerializeField] public int randomSeed = 1;

    [SerializeField] public int plateCount = 6;

    public Text loadingText;
    public Slider sliderBar;

    Mesh mesh;
    MeshFilter mf;
    MeshRenderer mr;

    Crust testCrust;
    Plate testPlate;

    float t;
    float coolingTime;
    float moveSpeed;
    float sliderProgress;

    bool cooling;
    bool move;

    void Start()
    {
        StartCoroutine(InitialisationCoroutine());
    }

    IEnumerator InitialisationCoroutine()
    {
        Random.InitState(randomSeed);
        mf = gameObject.AddComponent<MeshFilter>();
        mr = gameObject.AddComponent<MeshRenderer>();

        sliderProgress += 0.2f;
        yield return null;

        testCrust = new Crust(mf, mr, meshWidth, meshHeight, triWidth, triHeight, seaLevel: 1.0f);

        sliderProgress += 0.2f;
        yield return null;

        moveSpeed = 0.2f;
        coolingTime = 4;
        testCrust.Stage = new WaterStage();

        testCrust.BuildMesh(addNoise: true);

        sliderProgress += 0.2f;
        yield return null;

        testCrust.InitialiseCrust(plateCount);

        sliderProgress += 0.2f;
        yield return null;

        for (int i = 0; i < testCrust.Plates.Length; i++)
        {
            while (testCrust.Plates[i].XSpeed == 0 && testCrust.Plates[i].ZSpeed == 0)
            {
                testCrust.Plates[i].XSpeed = Random.Range(-2, 3);
                testCrust.Plates[i].ZSpeed = Random.Range(-2, 3);
                if (Random.Range(0.0f, 1.0f) > 0.5f)
                {
                    testCrust.Plates[i].Type = PlateType.Oceanic;
                }
                else
                {
                    testCrust.Plates[i].Type = PlateType.Continental;
                }
            }

            sliderProgress += (0.2f / testCrust.Plates.Length);
            yield return null;
        }

        sliderBar.gameObject.SetActive(false);
    }

    void Update()
    {
        float progress = Mathf.Clamp01(sliderProgress);
        sliderBar.value = progress;
        loadingText.text = Mathf.FloorToInt(progress * 100f) + "%";


        t = Time.deltaTime/coolingTime;

        if (Input.GetKeyDown("x"))
        {
            cooling = true;
        }

        if (cooling)
        {
            testCrust.SeaLevel -= t;
            Debug.Log("Sea level is now: " + (testCrust.SeaLevel * 100).ToString() + "%");
            testCrust.UpdateMesh();

            if(testCrust.SeaLevel <= -0.1f)
            {
                cooling = false;
            }
        }

        if (Input.GetKeyDown("z"))
        {
            testCrust.SeaLevel += 0.05f;
            Debug.Log("Sea level is now: " + (testCrust.SeaLevel * 100).ToString() + "%");
            testCrust.UpdateMesh();
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

            testCrust.UpdateMesh();
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
        testCrust.UpdateMesh();
    }

    public void PauseOrPlay(bool pause)
    {
        if (pause)
        {
            CancelInvoke();
        }
        else
        {
            InvokeRepeating("UpdateTestMesh", 0.0f, moveSpeed);
        }
    }

    private void OnDrawGizmos()
    {
        
        Gizmos.color = Color.red;
        /*
        Gizmos.DrawSphere(new Vector3(231, 150, 0), 0.5f);
        */
        /*
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