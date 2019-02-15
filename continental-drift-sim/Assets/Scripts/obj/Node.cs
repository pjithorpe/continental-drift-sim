using UnityEngine;

public class CrustNode : PoolableObject
{
    private Plate plate;
    private float height;
    private float density;
    private int x, z;
    private MaterialType type;
    private bool isVirtual;

    public CrustNode(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public Plate Plate
    {
        get { return this.plate; }
        set
        {
            this.plate = value;
            plate.NodeCount++;
        }
    }

    public float Height
    {
        get { return this.height; }
        set { this.height = value; }
    }

    public float Density
    {
        get { return this.density; }
        set { this.density = value; }
    }

    public int X
    {
        get { return this.x; }
        set { this.x = value; }
    }

    public int Z
    {
        get { return this.z; }
        set { this.z = value; }
    }

    public MaterialType Type
    {
        get { return this.type; }
        set { this.type = value; }
    }

    public bool IsVirtual
    {
        get { return this.isVirtual; }
        set { this.isVirtual = value; }
    }

    public void Copy(CrustNode nodeToCopy)
    {
        this.x = nodeToCopy.X;
        this.z = nodeToCopy.Z;
        this.plate = nodeToCopy.Plate;
        plate.NodeCount++;
        this.height = nodeToCopy.Height;
        this.density = nodeToCopy.Density;
        this.isVirtual = nodeToCopy.IsVirtual;
    }

    public override void CleanObject()
    {
        height = density = z = x = 0;
        plate.NodeCount--;
        plate = null;
    }
}

public enum MaterialType
{
    Oceanic,
    Continental
}