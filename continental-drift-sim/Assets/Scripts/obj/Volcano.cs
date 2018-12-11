using UnityEngine;
using UnityEditor;

public class Volcano : PoolableObject
{
    int x, z;
    int age;
    int materialRate;
    Crust crust;

    static int MAX_MATERIAL_PRODUCED = 400; // maximum material produced = maximum volcano age * maximum material rate

    public Volcano(int x, int z, Crust crust)
    {
        this.x = x;
        this.z = z;
        this.crust = crust;
        this.age = 0;
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
    public int Age
    {
        get { return this.age; }
        set { this.age = value; }
    }
    public int MaterialRate
    {
        get { return this.materialRate; }
        set { this.materialRate = value; }
    }
    public Crust Crust
    {
        get { return this.crust; }
        set { this.crust = value; }
    }

    public override void CleanObject()
    {
        materialRate = age = z = x = 0;
        crust = null;
    }

    private int[] GenerateNoise()
    {
        int[] noiseArray = new int[MAX_MATERIAL_PRODUCED];

        int offset = Random.Range(0, 100);
        for(int noiseIndex = 0; noiseIndex < noiseArray.Length; noiseIndex++)
        {
            noiseArray[noiseIndex] = (int)Mathf.PerlinNoise(0, 1);
        }

        return noiseArray;
    }
}