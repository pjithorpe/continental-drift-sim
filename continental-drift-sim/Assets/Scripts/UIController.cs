using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    // Start is called before the first frame update
    bool paused = true;
    Button reEnergiseBtn;
    Button randDirectionBtn;
    InputField seaLvlInputField;
    InputField volFreqInputField;

    void Start()
    {
        reEnergiseBtn = GameObject.Find("ReEnergiseButton").GetComponent<Button>();
        randDirectionBtn = GameObject.Find("RandomiseDirectionButton").GetComponent<Button>();
        seaLvlInputField = GameObject.Find("SeaLevelInputField").GetComponent<InputField>();
        volFreqInputField = GameObject.Find("VolFrequencyInputField").GetComponent<InputField>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ReEnergise()
    {
        MeshTest mainModule = GameObject.Find("CrustMesh").GetComponent<MeshTest>();
        mainModule.ReEnergisePlates();
    }

    public void Randomise()
    {
        MeshTest mainModule = GameObject.Find("CrustMesh").GetComponent<MeshTest>();
        mainModule.RandomisePlateMovements();
    }

    public void Pause(Text buttonText)
    {
        MeshTest mainModule = GameObject.Find("CrustMesh").GetComponent<MeshTest>();

        if (paused)
        {
            buttonText.text = "Pause";

            reEnergiseBtn.gameObject.SetActive(false);
            randDirectionBtn.gameObject.SetActive(false);
            seaLvlInputField.gameObject.SetActive(false);
            volFreqInputField.gameObject.SetActive(false);

            mainModule.PauseOrPlay(false, seaLvlInputField.text, volFreqInputField.text);
        }
        else
        {
            buttonText.text = "Play";

            reEnergiseBtn.gameObject.SetActive(true);
            randDirectionBtn.gameObject.SetActive(true);
            seaLvlInputField.gameObject.SetActive(true);
            volFreqInputField.gameObject.SetActive(true);

            mainModule.PauseOrPlay(true);
        }
        paused = !paused;
    }
}
