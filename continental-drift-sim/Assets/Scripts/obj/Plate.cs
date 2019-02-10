using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class Plate
{
    // field private vars
    private float defaultHeight = 5.0f;
    private int nodeCount = 0;
    private float mass = 0f;
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
    private Dictionary<Plate, int> affectors;
    private int energyBoostCount = 0;

    public Plate(float defaultHeight = 5.0f, Crust crust = null)
    {
        this.defaultHeight = defaultHeight;
        if (crust != null) { crust.AddPlate(this); }
        affectors = new Dictionary<Plate, int>();
    }


    public float DefaultHeight //default height of vertices inside the plate
    {
        get { return this.defaultHeight; }
        set { this.defaultHeight = value; }
    }
    public int NodeCount //number of nodes
    {
        get { return this.nodeCount; }
        set { this.nodeCount = value; }
    }
    public float Mass //number of nodes * density
    {
        get { return this.mass; }
        set { this.mass = value; }
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


    public void RecalculateMass()
    {
        mass = nodeCount * density;
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

    public void AffectPlateVector(Plate affectorPlate = null) //null for effects which increase plate speed that aren't a result of interacting with another plate
    {
        if (affectorPlate != null)
        {
            if (affectors.ContainsKey(affectorPlate))
            {
                affectors[affectorPlate]++;
            }
            else
            {
                affectors.Add(affectorPlate, 0);
            }
        }
        else
        {
            energyBoostCount++; //artificially add energy to plate
        }
    }
    public void ApplyVectorAffectors()
    {
        foreach(Plate p in affectors.Keys)
        {
            //Calculate force
            float proportionOfMass = (affectors[p] * p.Density) / p.Mass;
            float scaledAffectorXMomentum = proportionOfMass * p.AccurateXSpeed;
            float scaledAffectorZMomentum = proportionOfMass * p.AccurateZSpeed;

            //Apply force to plate
            float massRatioBetweenPlates = p.mass / this.mass;
            this.AccurateXSpeed += (scaledAffectorXMomentum * massRatioBetweenPlates);
            this.AccurateZSpeed += (scaledAffectorZMomentum * massRatioBetweenPlates);

            //Apply equal and opposite force to affector plate
            p.AccurateXSpeed -= scaledAffectorXMomentum;
            p.AccurateZSpeed -= scaledAffectorZMomentum;
        }

        // speed boost = current speed * (no. of boosting nodes / total nodes in plate) * boost constant
        if(energyBoostCount != 0)
        {
            float boostFactor = 8f * ((float)energyBoostCount / (float)nodeCount);
            float xBoostAmount = AccurateXSpeed * boostFactor;
            float zBoostAmount = AccurateZSpeed * boostFactor;

            AccurateXSpeed += xBoostAmount;
            AccurateZSpeed += zBoostAmount;
            energyBoostCount = 0;
        }
    }
}

public enum PlateType
{
    Oceanic,
    Continental
}