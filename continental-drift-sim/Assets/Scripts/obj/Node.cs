using UnityEngine;

public class Node : PoolableObject
{
    private Plate plate;
    private float height;
    private float density;
    private int x, z;

    public Node(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public Plate Plate
    {
        get { return this.plate; }
        set { this.plate = value; }
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

    public void Copy(Node nodeToCopy)
    {
        this.plate = nodeToCopy.Plate;
        this.height = nodeToCopy.Height;
        this.density = nodeToCopy.Density;
    }

    public override void CleanObject()
    {
        height = density = z = x = 0;
        plate = null;
    }
}