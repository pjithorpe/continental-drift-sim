using UnityEngine;
using UnityEditor;

public class Plate
{
    // field private vars
    private float defaultHeight = 5.0f;
    private int xSpeed = 0;
    private int zSpeed = 0;
    private Crust crust;

    // non-field definitions
    //not (get/set)able

    public Plate(float defaultHeight = 5.0f, int xSpeed = 0, int zSpeed = 0, Crust crust = null)
    {
        this.defaultHeight = defaultHeight;
        this.xSpeed = xSpeed;
        this.zSpeed = zSpeed;
        if (crust != null) { crust.AddPlate(this); }
    }


    public float DefaultHeight //default height of vertices inside the plate
    {
        get { return this.defaultHeight; }
        set { this.defaultHeight = value; }
    }
    public int XSpeed
    {
        get { return this.xSpeed; }
        set { this.xSpeed = value; }
    }
    public int ZSpeed
    {
        get { return this.zSpeed; }
        set { this.zSpeed = value; }
    }
    public Crust Crust
    {
        get { return this.crust; }
        set { this.crust = value; }
    }
}