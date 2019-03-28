using UnityEngine;
using UnityEditor;

/*
 * The class containing the attributes and methods for the Volcano object.
 *     X,Z <- The Volcano's position on the crust
 *     Age <- the number of iterations the Volcano has been active for
 *     MaterialRate <- The number of iterations of the particle deposition algorithm that should be performed in one global iteration for this Volcano
 *     NoiseArray <- array of floats randomly generated using 1D Perlin Noise. Used to make "random" decisions relating to the volcano
 *     CleanObject <- must
 */
public class Volcano : PoolableObject
{
    int x, z;
    int age;
    int materialRate;
	int[] noiseArray;
    Plate plate;

    static int MAX_MATERIAL_PRODUCED = 5000; //defnies the length of the noise function for the Volcano,
                                             //so this value more more than the maximum age of any volcano
                                             //multiplied by the maximum material rate of any volcano

    public Volcano(int x, int z, Plate plate, Crust crust)
    {
        this.x = x;
        this.z = z;
        this.plate = plate;
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
	public int[] NoiseArray
	{
		get { return this.noiseArray; }
		set { this.noiseArray = value; }
	}
    public Plate Plate
    {
        get { return this.plate; }
        set { this.plate = value; }
    }

    public override void CleanObject()
    {
        materialRate = age = z = x = 0;
        plate = null;
    }

    /*
     * Create an array of values randomly generated using 1D Perlin Noise
     */ 
    private int[] GenerateNoise()
    {
        int[] noiseArray = new int[MAX_MATERIAL_PRODUCED];

		float perlinFactor = 1.0f;
		float offset = Random.Range(0, 100) + Random.Range(0.1f, 0.99f);
        for(int noiseIndex = 0; noiseIndex < noiseArray.Length; noiseIndex++)
        {
			float noiseX = noiseIndex * perlinFactor;
			if(noiseX == (int)noiseX)
			{
				noiseX += 0.00001f;
			}
            noiseArray[noiseIndex] = (int)Mathf.PerlinNoise(noiseX, offset);
        }

        return noiseArray;
    }
}