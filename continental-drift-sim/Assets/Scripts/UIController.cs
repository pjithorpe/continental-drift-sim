using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    // Start is called before the first frame update
    bool paused = true;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Pause(Text buttonText)
    {
        MeshTest mainModule = GameObject.Find("CrustMesh").GetComponent<MeshTest>();
        if (paused)
        {
            buttonText.text = "Pause";
            mainModule.PauseOrPlay(false);
        }
        else
        {
            buttonText.text = "Play";
            mainModule.PauseOrPlay(true);
        }
        paused = !paused;
    }
}
