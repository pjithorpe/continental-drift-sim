using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartSimulation()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void QuitApplication()
    {
        Application.Quit();
    }
}