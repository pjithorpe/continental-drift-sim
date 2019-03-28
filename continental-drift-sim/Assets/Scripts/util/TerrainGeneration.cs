using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TerrainGeneration
{
    private static int w;
    private static int h;

    private static float GetHeightAtPoint(int x, int z, ref float[] fractal)
    {
        if (x < 0) { x = w + x; }
        if (z < 0) { z = h + z; }

        return fractal[(x % (w - 1)) + (z % (h - 1)) * w];
    }

    private static void SetHeightAtPoint(int x, int z, float value, ref float[] fractal)
    {
        if (x < 0) { x = w + x; }
        if (z < 0) { z = h + z; }

        fractal[(x % (w - 1)) + (z % (h - 1)) * w] = value;
    }

    private static void SquareStep(int x, int z, int size, float value, ref float[] fractal)
    {
        int halfSize = size / 2;

        float a = GetHeightAtPoint(x - halfSize, z - halfSize, ref fractal);
        float b = GetHeightAtPoint(x + halfSize, z - halfSize, ref fractal);
        float c = GetHeightAtPoint(x - halfSize, z + halfSize, ref fractal);
        float d = GetHeightAtPoint(x + halfSize, z + halfSize, ref fractal);

        float meanHeight = (a + b + c + d) / 4.0f;
        float adjustedHeight = meanHeight + value;

        SetHeightAtPoint(x, z, adjustedHeight, ref fractal);
    }

    private static void DiamondStep(int x, int z, int size, float value, ref float[] fractal)
    {
        int halfSize = size / 2;

        float a = GetHeightAtPoint(x - halfSize, z, ref fractal);
        float b = GetHeightAtPoint(x + halfSize, z, ref fractal);
        float c = GetHeightAtPoint(x, z - halfSize, ref fractal);
        float d = GetHeightAtPoint(x, z + halfSize, ref fractal);

        float meanHeight = (a + b + c + d) / 4.0f;
        float adjustedHeight = meanHeight + value;

        SetHeightAtPoint(x, z, adjustedHeight, ref fractal);
    }

    private static void DiamondSquare(int stepSize, float scale, ref float[] fractal)
    {
        int halfStep = stepSize / 2;
        for (int z = halfStep; z < h + halfStep; z += stepSize)
        {
            for (int x = halfStep; x < w + halfStep; x += stepSize)
            {
                SquareStep(x, z, stepSize, Random.Range(-1.0f, 1.0f) * scale, ref fractal);
            }
        }

        for (int z = 0; z < h; z += stepSize)
        {
            for (int x = 0; x < w; x += stepSize)
            {
                DiamondStep(x + halfStep, z, stepSize, Random.Range(-1.0f, 1.0f) * scale, ref fractal);
                DiamondStep(x, z + halfStep, stepSize, Random.Range(-1.0f, 1.0f) * scale, ref fractal);
            }
        }


    }

    /*
     * Generate a fractal map using diamond square algorithm and store in fractal[]
     */
    private static float[] GenerateFractal(int size, float maxHeight, int featureSize)
    {
        float[] fractal = new float[size * size];
        w = size;
        h = size;

        for (int z = featureSize - 1; z < size; z += featureSize)
        {
            for (int x = featureSize - 1; x < size; x += featureSize)
            {
                SetHeightAtPoint(x, z, Random.Range(0.35f * maxHeight, 0.65f * maxHeight), ref fractal);
            }
        }

        int sampleSize = featureSize;
        float randomScale = 1.0f;

        while (sampleSize > 1)
        {
            DiamondSquare(sampleSize, randomScale, ref fractal);

            sampleSize = sampleSize / 2;
            randomScale = randomScale / 2.0f;
        }

        /*TODO: Now go around the edges and apply a fucntion that will cause them to be always be ocean  so that we don't get odd straight sided continents
        for (int x = 0; x < height; x += featureSize)
        {
            for (int z = 0; z < height; z += featureSize)
            {

            }
        }*/

        return fractal;
    }

    private static float[] GeneratePerlinNoise(int sizeX, int sizeZ)
    {
        float[] perlinNoise = new float[sizeX * sizeZ];

        //Precalculating floats which are used in loop
        float perlinFraction = 0.0001f; // Have to add a small fraction for Mathf.PerlinNoise to work
        float offset = Random.Range(10, 100) + Random.Range(0.1f, 0.99f);

        for (int i = 0; i < sizeX*sizeZ; i++)
        {
            int xPos = i % sizeX;
            int zPos = i / sizeX;

            perlinNoise[i] = Mathf.PerlinNoise((xPos / perlinFraction) + offset, (zPos / perlinFraction) + offset);
        }

        return perlinNoise;
    }

    public static Crust ApplyFractalToCrust(Crust crust)
    {
        //Generate fractal landscape
        float[] fractalHeights = GenerateFractal(crust.Width, crust.MaxHeight, crust.Width / 8);

        //Add Perlin noise to seabed
        float[] perlinHeights = GeneratePerlinNoise(crust.Width, crust.Height);

        Vector3[] verts = crust.Mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            int xPos = i % crust.Width;
            int zPos = i / crust.Width;

            float y = fractalHeights[i] * crust.MaxHeight;
            
            if (y < crust.SeaLevel)
            {
                y = crust.SeaLevel - (crust.MaxHeight * 0.025f); //add a little ridge
                crust.CrustNodes[xPos,zPos][0].Type = MaterialType.Oceanic;
            }
            else
            {
                y = crust.SeaLevel + ((y - crust.SeaLevel) * 0.5f);
                crust.CrustNodes[xPos, zPos][0].Type = MaterialType.Continental;
            }

            crust.CrustNodes[xPos, zPos][0].Height = y;
        }
        Debug.Log("in apply: " + crust.CrustNodes[30, 222][0].Height);
        return crust;
    }


    public static List<CrustNode>[,] ParticleDeposition(List<CrustNode>[,] nodes, Volcano vol, float rockSize, float heightSimilarityEpsilon, int dropZoneRadius, int maxSearchRange, int maxElevationThreshold)
    {
        int width = nodes.GetLength(0);
        int height = nodes.GetLength(1);
        int rocksThrown = vol.Age * vol.MaterialRate;
        int searchRange = 0; //higher search radius will make shallower slopes
        int elevationThreshold = 0; //higher elevation threshold than 1 will make very steep slopes
        float differenceFactor = heightSimilarityEpsilon;
        for (int rock = 0; rock < vol.MaterialRate; rock++)
        {
            //choose a random drop point within volcano's radius
            float dropPointAngle = 2 * Mathf.PI * Random.Range(0.0f, 1.0f);
            float dropPointDistance = dropZoneRadius * Random.Range(0.0f, 1.0f);
            float relativeDropX = Mathf.Cos(dropPointAngle) * dropPointDistance;
            float relativeDropZ = Mathf.Sin(dropPointAngle) * dropPointDistance;
            int dropX = Mathf.RoundToInt(vol.X + relativeDropX);
            int dropZ = Mathf.RoundToInt(vol.Z + relativeDropZ);

            int currentX = dropX % width;
            int currentZ = dropZ % height;
            if (currentX < 0) { currentX = width + currentX; }
            if (currentZ < 0) { currentZ = height + currentZ; }

            int noiseIndex = rocksThrown + rock;
            searchRange = (int)(maxSearchRange * vol.NoiseArray[noiseIndex]) + 1; //should give range values from 1 to maxSearchRange ( and maxSearchRange+1 in some rare cases)
            if (maxElevationThreshold != 0)
            {
                elevationThreshold = (int)(maxElevationThreshold * vol.NoiseArray[vol.NoiseArray.Length - noiseIndex]); //should give values 0 to maxElevationThreshold-1 (2 maxElevationThreshold)
                differenceFactor = elevationThreshold + heightSimilarityEpsilon;
            }

            //rock rolling loop
            bool stable = false;
            while (!stable)
            {
                for (int range = 1; range <= searchRange; range++)
                {
                    //do hexagon search and randomise side priority

                    bool diagnonalMove = true; //Start with a diagonal move when searching from an odd row
                    if (currentZ % 2 == 0)
                    {
                        diagnonalMove = false;
                    }

                    int side = Random.Range(0, 3);
                    int searchX, searchZ;
                    switch (side) //Move search markers
                    {
                        case 0:
                            searchX = -range; searchZ = 0;
                            break;
                        case 1:
                            if (diagnonalMove) { searchX = ((range - 1) / 2 + 1) - range; }
                            else { searchX = range / 2 - range; }
                            searchZ = range;
                            break;
                        default: //case 2
                            if (diagnonalMove) { searchX = range / 2 - range; }
                            else { searchX = ((range - 1) / 2 + 1) - range; }
                            searchZ = range;
                            break;
                    }

                    stable = true;
                    for (int s = 0; s < 3; s++) //3 sets of parallel sides
                    {
                        for (int c = 0; c < range; c++)
                        {
                            var currentNode = nodes[currentX, currentZ][0];
                            int crustIndexX = (currentX + searchX) % width;
                            if (crustIndexX < 0) { crustIndexX = width + crustIndexX; }
                            if (Random.value > 0.5f)//Randomise which side to check first (we don't want to bias the drops)
                            {
                                int crustIndexZ = (currentZ + searchZ) % height;
                                if (crustIndexZ < 0) { crustIndexZ = height + crustIndexZ; }
                                var topHexagonNode = nodes[crustIndexX, crustIndexZ][0];

                                if (currentNode.Height - topHexagonNode.Height > differenceFactor)
                                {
                                    currentX = crustIndexX;
                                    currentZ = crustIndexZ;
                                    stable = false;
                                }
                                else
                                {
                                    var botHexagonNode = nodes[crustIndexX, crustIndexZ][0];

                                    if (currentNode.Height - botHexagonNode.Height > differenceFactor)
                                    {
                                        currentX = crustIndexX;
                                        currentZ = crustIndexZ;
                                        stable = false;
                                    }
                                }
                            }
                            else
                            {
                                int crustIndexZ = (currentZ - searchZ) % height;
                                if (crustIndexZ < 0) { crustIndexZ = height + crustIndexZ; }
                                var botHexagonNode = nodes[crustIndexX, crustIndexZ][0];

                                if (currentNode.Height - botHexagonNode.Height > differenceFactor)
                                {
                                    currentX = crustIndexX;
                                    currentZ = crustIndexZ;
                                    stable = false;
                                }
                                else
                                {
                                    crustIndexZ = (currentZ + searchZ) % height;
                                    if (crustIndexZ < 0) { crustIndexZ = height + crustIndexZ; }
                                    var topHexagonNode = nodes[crustIndexX, crustIndexZ][0];

                                    if (currentNode.Height - topHexagonNode.Height > differenceFactor)
                                    {
                                        currentX = crustIndexX;
                                        currentZ = crustIndexZ;
                                        stable = false;
                                    }
                                }
                            }

                            switch (side) //Move search markers
                            {
                                case 0:
                                    if (diagnonalMove) { searchX++; }
                                    searchZ++;
                                    diagnonalMove = !diagnonalMove;
                                    break;
                                case 1:
                                    searchX++;
                                    break;
                                case 2:
                                    if (diagnonalMove) { searchX++; }
                                    searchZ--;
                                    diagnonalMove = !diagnonalMove;
                                    break;
                            }
                        }

                        if (stable) { break; }
                        side++;
                        if (side > 2)
                        {
                            side = 0;
                            searchX = -range;
                            searchZ = 0;
                        }
                    }

                    if (stable) { break; }
                }
            }

            //drop the rock
            nodes[currentX, currentZ][0].Height += rockSize;
            nodes[currentX, currentZ][0].Type = MaterialType.Continental;
            if (vol.Plate != null)
            {
                nodes[currentX, currentZ][0].Plate = vol.Plate;
            }
        }

        return nodes;
    }
}
