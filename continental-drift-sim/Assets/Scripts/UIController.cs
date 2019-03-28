using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    bool paused = true;
    Button reEnergiseBtn;
    Button randDirectionBtn;
    InputField seaLvlInputField;
    InputField volFreqInputField;

    // Start is called before the first frame update
    void Start()
    {
        reEnergiseBtn = GameObject.Find("ReEnergiseButton").GetComponent<Button>();
        randDirectionBtn = GameObject.Find("RandomiseDirectionButton").GetComponent<Button>();
        seaLvlInputField = GameObject.Find("SeaLevelInputField").GetComponent<InputField>();
        volFreqInputField = GameObject.Find("VolFrequencyInputField").GetComponent<InputField>();
    }

    public void ReEnergise()
    {
        SimulationDriver mainModule = GameObject.Find("CrustMesh").GetComponent<SimulationDriver>();
        mainModule.ReEnergisePlates();
    }

    public void Randomise()
    {
        SimulationDriver mainModule = GameObject.Find("CrustMesh").GetComponent<SimulationDriver>();
        mainModule.RandomisePlateMovements();
    }

    public void Save()
    {
        SimulationDriver mainModule = GameObject.Find("CrustMesh").GetComponent<SimulationDriver>();
        mainModule.SaveMap();
    }

    public void Pause(Text buttonText)
    {
        SimulationDriver mainModule = GameObject.Find("CrustMesh").GetComponent<SimulationDriver>();

        if (paused)
        {
            //Show "playing" UI
            buttonText.text = "Pause";

            reEnergiseBtn.gameObject.SetActive(false);
            randDirectionBtn.gameObject.SetActive(false);
            seaLvlInputField.gameObject.SetActive(false);
            volFreqInputField.gameObject.SetActive(false);

            mainModule.PauseOrPlay(false, seaLvlInputField.text, volFreqInputField.text);
        }
        else
        {
            //Show "paused" UI
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
