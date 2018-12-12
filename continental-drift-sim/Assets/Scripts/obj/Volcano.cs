﻿using UnityEngine;
using UnityEditor;

public class Volcano : PoolableObject
{
    int x, z;
    int age;
    int materialRate;
    Crust crust;
	int[] noiseArray;

    static int MAX_MATERIAL_PRODUCED = 400; // maximum material produced = maximum volcano age * maximum material rate

    public Volcano(int x, int z, Crust crust)
    {
        this.x = x;
        this.z = z;
        this.crust = crust;
        this.age = 0;
		this.noiseArray = GenerateNoise();
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
	public int[] NoiseArray
	{
		get { return this.noiseArray; }
		set { this.noiseArray = value; }
	}

    public override void CleanObject()
    {
        materialRate = age = z = x = 0;
        crust = null;
    }

    private int[] GenerateNoise()
    {
        int[] noiseArray = new int[MAX_MATERIAL_PRODUCED];

		float perlinFactor = 1.0f;
		int offset = Random.Range(0, 100) + Random.Range(0.1f, 0.99f);
        for(int noiseIndex = 0; noiseIndex < noiseArray.Length; noiseIndex++)
        {
			float noiseX = noiseIndex * perlinFactor;
			if(noiseX = (int)noiseX)
			{
				noiseX += 0.00001f;
			}
            noiseArray[noiseIndex] = (int)Mathf.PerlinNoise(noiseX, offset);
        }

        return noiseArray;
    }
}