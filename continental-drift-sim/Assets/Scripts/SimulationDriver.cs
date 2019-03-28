using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimulationDriver : MonoBehaviour
{

    [SerializeField] public float triSize = 1f;

    [SerializeField] int meshWidth = 512;
    [SerializeField] public int meshHeight = 512;

    [SerializeField] public int randomSeed = 1;

    [SerializeField] public int plateCount = 6;
    [SerializeField] public int voronoiRelaxationSteps = 0;

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
        // Now set the camera dimensions
        Camera mainCam = Camera.main;
        mainCam.enabled = true;
        mainCam.aspect = 1;
        mainCam.transform.position = new Vector3(meshWidth * 0.5f, 50.0f, meshHeight * 0.5f);
        //This enables the orthographic mode
        mainCam.orthographic = true;
        //Set the size of the viewing volume you'd like the orthographic Camera to pick up (5)
        mainCam.orthographicSize = meshWidth * 0.5f;
        //Set the orthographic Camera Viewport size and position
        mainCam.rect = new Rect(0.0f, 0.0f, meshWidth, meshHeight);

        StartCoroutine(InitialisationCoroutine());
    }

    IEnumerator InitialisationCoroutine()
    {
        moveSpeed = 0.15f;

        //Random.InitState(randomSeed); uncomment to seed simulation
        mf = gameObject.AddComponent<MeshFilter>();
        mr = gameObject.AddComponent<MeshRenderer>();

        sliderProgress += 0.2f;
        yield return null;

        testCrust = new Crust(meshWidth, meshHeight, triSize, seaLevel: 1.0f);

        sliderProgress += 0.2f;
        yield return null;

        testCrust.Mesh = MeshBuilder.BuildMesh(mf, mr, meshWidth, meshHeight, triSize);

        testCrust = TerrainGeneration.ApplyFractalToCrust(testCrust);
        Debug.Log("in apply: " + testCrust.CrustNodes[30, 222][0].Height);

        sliderProgress += 0.2f;
        yield return null;

        testCrust = PlateGeneration.SplitCrustIntoPlates(testCrust, meshWidth, meshHeight, plateCount, voronoiRelaxationSteps);

        sliderProgress += 0.2f;
        yield return null;

        for (int i = 0; i < testCrust.Plates.Length; i++)
        {
            while ((testCrust.Plates[i].XSpeed < 0.3f && testCrust.Plates[i].XSpeed > -0.3f) && (testCrust.Plates[i].ZSpeed < 0.3f && testCrust.Plates[i].ZSpeed > -0.3f))
            {
                testCrust.Plates[i].AccurateXSpeed = Random.Range(-2f, 2f);
                testCrust.Plates[i].AccurateZSpeed = Random.Range(-2f, 2f);
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
    }

    void UpdateTestMesh()
    {
        Mesh mesh = testCrust.UpdateMesh();
        mf.mesh = mesh;
    }

    public void PauseOrPlay(bool pause, string seaLevelTxt = null, string volFreqTxt = null)
    {
        if (pause)
        {
            CancelInvoke();
        }
        else
        {
            if (seaLevelTxt != null)
            {
                float temp;
                float.TryParse(seaLevelTxt, out temp);
                testCrust.SeaLevel = temp;
            }
            if (volFreqTxt != null)
            {
                float temp;
                float.TryParse(volFreqTxt, out temp);
                testCrust.VolcanoFrequency = temp;
            }
            InvokeRepeating("UpdateTestMesh", 0.0f, moveSpeed);
        }
    }

    public void ReEnergisePlates()
    {
        testCrust.ReEnergisePlates();
    }

    public void RandomisePlateMovements()
    {
        testCrust.RandomisePlateMovements();
    }

    public void SaveMap()
    {
        testCrust.SaveMapToPNG();
    }
}