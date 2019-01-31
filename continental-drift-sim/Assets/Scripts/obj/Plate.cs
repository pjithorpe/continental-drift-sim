using UnityEngine;
using UnityEditor;

public class Plate
{
    // field private vars
    private float defaultHeight = 5.0f;
    private float xSpeed = 0;
	private float zSpeed = 0;
	private float absoluteInverseXSpeed = 0;
	private float absoluteInverseZSpeed = 0;
	private bool checkMoveX = false;
	private bool checkMoveZ = false;
    private float density = 0.0f;
    private PlateType type;
    private Crust crust;

    // non-field definitions
    //not (get/set)able
	private int xMoveCounter = 0;
	private int zMoveCounter = 0;

    public Plate(float defaultHeight = 5.0f, Crust crust = null)
    {
        this.defaultHeight = defaultHeight;
        if (crust != null) { crust.AddPlate(this); }
    }


    public float DefaultHeight //default height of vertices inside the plate
    {
        get { return this.defaultHeight; }
        set { this.defaultHeight = value; }
    }

    public int XSpeed
    {
        get
		{
			if (xSpeed > 1)
			{
				if (xSpeed > 0)
				{
					return 1;
				}
				else
				{
					return -1;
				}
			}
			return Mathf.RoundToInt(xSpeed);
		}
    }
	public int ZSpeed
	{
		get
		{
			if (zSpeed > 1)
			{
				if (zSpeed > 0)
				{
					return 1;
				}
				else
				{
					return -1;
				}
			}
			return Mathf.RoundToInt(zSpeed);
		}
	}
	public float AccurateXSpeed
	{
		get { return this.xSpeed; }
		set
		{
			this.xSpeed = value;
			absoluteInverseXSpeed = Mathf.Abs(1f / xSpeed);
		}
	}
	public float AccurateZSpeed
	{
		get { return this.zSpeed; }
		set
		{
			this.zSpeed = value;
			absoluteInverseZSpeed = Mathf.Abs(1f / zSpeed);
		}
	}

    public float Density
    {
        get { return this.density; }
        set { this.density = value; }
    }
    public PlateType Type
    {
        get { return this.type; }
        set { this.type = value; }
    }

    public Crust Crust
    {
        get { return this.crust; }
        set { this.crust = value; }
    }


	public void RegisterMovement()
	{
		xMoveCounter++;
		zMoveCounter++;

		if (xMoveCounter >= absoluteInverseXSpeed)
		{
			checkMoveX = true;
			xMoveCounter = 0;
		}
		else
		{
			checkMoveX = false;
		}
		if (xMoveCounter >= absoluteInverseXSpeed)
		{
			checkMoveZ = true;
			zMoveCounter = 0;
		}
		else
		{
			checkMoveZ = false;
		}
	}
	public bool CheckMoveX()
	{
		return checkMoveX;
	}
	public bool CheckMoveZ()
	{
		return checkMoveZ;
	}
}

public enum PlateType
{
    Oceanic,
    Continental
}