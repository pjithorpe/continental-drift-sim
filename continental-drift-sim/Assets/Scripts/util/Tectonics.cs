using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class Tectonics
{
    public static void PlateInteraction(int xPos, int zPos, Crust crust, ref List<CrustNode>[,] nodes, ref LinkedList<CrustNode>[,] movedNodes, ref Dictionary<Plate, int> singlePlateSpacesCounts, float subductionFactor, float subductionVolcanoDepthThreshold, float volcanoFrequency)
    {
        //plates are overlapping, check which should subduct underneath (virtual), and which should be the surface (non-virtual)

        // check if there is more than one non-virtual plate
        bool oneNonVirtual = false;
        float nonVirtualHeight = 0f;
        var currentNode = movedNodes[xPos, zPos].First; // note: this refers to a LinkedListNode<T>, not a CrustNode
        for (int k = 0; k < movedNodes[xPos, zPos].Count; k++)
        {
            if (!currentNode.Value.IsVirtual)
            {
                if (oneNonVirtual) { oneNonVirtual = false; break; } //multiple non-virtual nodes, break and go to plate interaction logic
                else
                {
                    oneNonVirtual = true;
                    nonVirtualHeight = currentNode.Value.Height;
                }
            }
            currentNode = currentNode.Next;
        }

        if (oneNonVirtual) //one non-virtual node
        {
            //subduct all virtual nodes downwards
            currentNode = movedNodes[xPos, zPos].First;
            for (int i = 0; i < movedNodes[xPos, zPos].Count; i++)
            {
                if (currentNode.Value.IsVirtual)
                {
                    currentNode.Value.Height = currentNode.Value.Height - subductionFactor; //subduct downwards

                    //If the subducting plate is deep enough, random chance of new volcano
                    if (movedNodes[xPos, zPos].Last.Value.Height < nonVirtualHeight - subductionVolcanoDepthThreshold)
                    {
                        float chance = Random.Range(0.0f, 1.0f);
                        if (chance < 0.0001f * volcanoFrequency)
                        {
                            Volcano v = ObjectPooler.current.GetPooledVolcano();
                            v.X = xPos;
                            v.Z = zPos;
                            v.MaterialRate = Random.Range(30, 60); //How many rocks get thrown out of the volcano each frame
                            crust.AddStratoVolcano(v); //steep sided volcano
                        }
                    }
                }
                currentNode = currentNode.Next;
            }
        }
        else //multiple non-virtual nodes
        {
            CollidePlates(xPos, zPos, ref movedNodes, ref crust, ref singlePlateSpacesCounts, subductionFactor);
        }

        //Clear space in main array
        int listLength = nodes[xPos, zPos].Count;
        for (int i = 0; i < listLength; i++)
        {
            ObjectPooler.current.ReturnNodeToPool(nodes[xPos, zPos][i]);
        }
        nodes[xPos, zPos].Clear();
        //crustNodes[xPos, zPos].TrimExcess();  <-- lower memory usage, higher processing cost

        //Place moved nodes into freed space
        currentNode = movedNodes[xPos, zPos].First;
        listLength = movedNodes[xPos, zPos].Count;
        for (int i = 0; i < listLength; i++)
        {
            //get rid of virtual nodes that are below the surface
            if (!(currentNode.Value.IsVirtual && currentNode.Value.Height < 0.0f))
            {
                var movedCrustNode = ObjectPooler.current.GetPooledNode();
                movedCrustNode.Copy(currentNode.Value);

                nodes[xPos, zPos].Add(movedCrustNode);
            }
            currentNode = currentNode.Next;
        }
    }

    private static void CollidePlates(int xPos, int zPos, ref LinkedList<CrustNode>[,] movedNodes, ref Crust crust, ref Dictionary<Plate, int> singlePlateSpacesCounts, float subductionFactor) //TODO: None of these methods properly consider Virtual nodes yet (need to change this!)
    {
        int width = movedNodes.GetLength(0);
        int height = movedNodes.GetLength(1);

        bool hasOceanic = false, hasContinental = false, hasMultipleOc = false, hasMultipleCo = false;

        var currentNode = movedNodes[xPos, zPos].First;
        for (int k = 0; k < movedNodes[xPos, zPos].Count; k++)
        {
            //count oceanic/continental
            if (currentNode.Value.Type == MaterialType.Oceanic)
            {
                if (hasOceanic != true) { hasOceanic = true; }
                else { hasMultipleOc = true; }
            }
            else
            {
                if (hasContinental != true) { hasContinental = true; }
                else { hasMultipleCo = true; }
            }


            //also (if non-virtual), cause nodes to affect eachother's plate's speeds
            if (!currentNode.Value.IsVirtual)
            {
                var affectedNode = movedNodes[xPos, zPos].First;
                for (int n = 0; n < movedNodes[xPos, zPos].Count; n++)
                {
                    if (n != k) // don't affect itself
                    {
                        affectedNode.Value.Plate.AffectPlateVector(currentNode.Value.Plate);
                    }
                    affectedNode = affectedNode.Next;
                }
            }

            currentNode = currentNode.Next;
        }

        int listLength;
        //if O-O, Lowest density plate subducts
        if (!hasContinental)
        {
            int mostDense = 0;
            float highestDensity = 0.0f;
            currentNode = movedNodes[xPos, zPos].First;
            for (int k = 0; k < movedNodes[xPos, zPos].Count; k++)
            {
                if (currentNode.Value.Density > highestDensity)
                {
                    mostDense = k;
                    highestDensity = currentNode.Value.Density;
                }
                currentNode = currentNode.Next;
            }

            currentNode = movedNodes[xPos, zPos].First;
            listLength = movedNodes[xPos, zPos].Count;
            for (int k = 0; k < listLength; k++)
            {
                if (k != mostDense)
                {
                    currentNode.Value.IsVirtual = true;
                    currentNode.Value.Height = currentNode.Value.Height - subductionFactor; //subduct downwards
                    currentNode = currentNode.Next;
                }
                else
                {
                    currentNode.Value.IsVirtual = false;
                    if (k != 0) //no need to move it to the start if it's already there
                    {
                        movedNodes[xPos, zPos].AddFirst(currentNode.Value);
                        var nodeToDelete = currentNode;
                        currentNode = currentNode.Next;
                        movedNodes[xPos, zPos].Remove(nodeToDelete);
                    }
                }
            }
        }
        //if C-C, crunch
        else if (!hasOceanic)
        {
            //TEMPORARY, JUST USE THE OLD NAIVE CRUNCH
            currentNode = movedNodes[xPos, zPos].First;
            CrustNode fastestPlateNode = null;
            float highestAggregateVelocity = 0f;
            for (int k = 0; k < movedNodes[xPos, zPos].Count; k++)
            {
                //find fastest plate
                float aggregateVelocity = Math.Abs(currentNode.Value.Plate.AccurateXSpeed) + Math.Abs(currentNode.Value.Plate.AccurateZSpeed);
                if (aggregateVelocity > highestAggregateVelocity)
                {
                    fastestPlateNode = currentNode.Value;
                    highestAggregateVelocity = aggregateVelocity;
                }

                //Add all present plates to dict to be used later for plate assignment
                if (!singlePlateSpacesCounts.ContainsKey(currentNode.Value.Plate))
                {
                    singlePlateSpacesCounts.Add(currentNode.Value.Plate, 0);
                }
                currentNode = currentNode.Next;
            }

            currentNode = movedNodes[xPos, zPos].First;
            for (int k = 0; k < movedNodes[xPos, zPos].Count; k++)
            {
                if (currentNode.Value != fastestPlateNode && Random.Range(0.0f, 1.0f) > 0.99f)
                {
                    //add together the speeds of both plates relative to eachother to see how big the impact and resultant crumpling should be
                    float comparativeX = fastestPlateNode.Plate.AccurateXSpeed - currentNode.Value.Plate.AccurateXSpeed; // (we flip the smaller vector for comparison)
                    float comparativeZ = fastestPlateNode.Plate.AccurateZSpeed - currentNode.Value.Plate.AccurateZSpeed;

                    //now get the magnitude of this comparison vector and this will represent the power of the collision
                    float collisionMagnitude = Mathf.Sqrt(comparativeX * comparativeX + comparativeZ * comparativeZ);

                    //Send out a pulse from this node in the opposite direction to the movement of the slower plate (could change all this later to consider weight/density/force etc)
                    int pulseDistance = Mathf.RoundToInt(collisionMagnitude) * Random.Range(100, 201);
                    int halfPulseDistance = pulseDistance / 2;
                    int xLoc = xPos;
                    int zLoc = zPos;
                    int dx = 1;
                    int dz = 1;
                    int moveCount = 0;

                    if (currentNode.Value.Plate.AccurateXSpeed < 0)
                    {
                        dx = -1;
                    }
                    if (currentNode.Value.Plate.AccurateZSpeed < 0)
                    {
                        dz = -1;
                    }

                    if (currentNode.Value.Plate.ScaledVectorX == 1f)
                    {
                        for (int p = 0; p < pulseDistance; p++)
                        {
                            moveCount++;

                            xLoc += dx;
                            xLoc = xLoc % width;
                            if (xLoc < 0) { xLoc = width + xLoc; }

                            if (moveCount >= (1.0f / currentNode.Value.Plate.ScaledVectorZ))
                            {
                                zLoc += dz;
                                zLoc = zLoc % height;
                                if (zLoc < 0) { zLoc = height + zLoc; }

                                moveCount = 0;
                            }
                        }
                    }
                    else
                    {
                        for (int p = 0; p < pulseDistance; p++)
                        {
                            moveCount++;

                            zLoc += dz;
                            zLoc = zLoc % height;
                            if (zLoc < 0) { zLoc = height + zLoc; }

                            if (moveCount >= (1.0f / currentNode.Value.Plate.ScaledVectorX))
                            {
                                xLoc += dx;
                                xLoc = xLoc % width;
                                if (xLoc < 0) { xLoc = width + xLoc; }

                                moveCount = 0;
                            }
                        }
                    }

                    if (Random.Range(0.0f, 1.0f) < 0.9f)
                    {
                        Volcano v = ObjectPooler.current.GetPooledVolcano();
                        v.X = xPos;
                        v.Z = zPos;
                        v.Plate = currentNode.Value.Plate;
                        v.MaterialRate = Random.Range(10, 150); //How many rocks get thrown out of the volcano each frame
                        crust.AddStratoVolcano(v);
                    }
                    else //random chance of a huge mountain
                    {
                        Volcano v = ObjectPooler.current.GetPooledVolcano();
                        v.X = xPos;
                        v.Z = zPos;
                        v.Plate = currentNode.Value.Plate;
                        v.MaterialRate = Random.Range(250, 300); //How many rocks get thrown out of the volcano each frame
                        crust.AddStratoVolcano(v);
                    }
                }
            }


            //assign nodes to plates
            int searchDistance = 0;
            bool found = false;
            while (!found)
            {
                searchDistance++; // hexagon side length = searchDistance

                //It is best to search in a hexagon outline shape. This algorithm will have to be horizontally flipped if working on an odd z value row
                bool diagnonalMove = true; //Start with a diagonal move when searching from an odd row
                if (zPos % 2 == 0)
                {
                    diagnonalMove = false;
                }
                //start at the left corner and move diagonally up, then across to the right, then diagonally down to the right corner
                //at the same time, do the mirror opposite for the bottom half of the hexagon
                int x, z;
                x = -searchDistance;
                z = 0;
                // up/down from left corner
                for (int c = 0; c < searchDistance; c++)
                {
                    CheckSpaceAndUpdateDict(xPos + x, zPos + z, width, height, ref movedNodes, ref singlePlateSpacesCounts, ref found); //top half
                    CheckSpaceAndUpdateDict(xPos + x, zPos - z, width, height, ref movedNodes, ref singlePlateSpacesCounts, ref found); //bottom half
                    if (diagnonalMove) { x++; }
                    z++;
                    diagnonalMove = !diagnonalMove;
                }
                // across top/bottom
                for (int c = 0; c < searchDistance; c++)
                {
                    CheckSpaceAndUpdateDict(xPos + x, zPos + z, width, height, ref movedNodes, ref singlePlateSpacesCounts, ref found);
                    CheckSpaceAndUpdateDict(xPos + x, zPos - z, width, height, ref movedNodes, ref singlePlateSpacesCounts, ref found);
                    x++;
                }
                // down/up to right corner
                for (int c = 0; c < searchDistance; c++)
                {
                    CheckSpaceAndUpdateDict(xPos + x, zPos + z, width, height, ref movedNodes, ref singlePlateSpacesCounts, ref found);
                    CheckSpaceAndUpdateDict(xPos + x, zPos - z, width, height, ref movedNodes, ref singlePlateSpacesCounts, ref found);
                    if (diagnonalMove) { x++; }
                    z--;
                    diagnonalMove = !diagnonalMove;
                }
            }

            Plate closestPlate = null;
            foreach (Plate plt in singlePlateSpacesCounts.Keys)
            {
                if (closestPlate == null || singlePlateSpacesCounts[plt] > singlePlateSpacesCounts[closestPlate])
                {
                    closestPlate = plt;
                }
            }
            singlePlateSpacesCounts.Clear();

            CrustNode chosenNode = movedNodes[xPos, zPos].First.Value;
            currentNode = movedNodes[xPos, zPos].First;
            listLength = movedNodes[xPos, zPos].Count;
            for (int k = 0; k < listLength; k++)
            {
                if (currentNode.Value.Plate == closestPlate)
                {
                    movedNodes[xPos, zPos].AddFirst(currentNode.Value);
                    var nodeToDelete = currentNode;
                    currentNode = currentNode.Next;
                    movedNodes[xPos, zPos].Remove(nodeToDelete);
                }
                else
                {
                    //Remove
                    ObjectPooler.current.ReturnNodeToPool(currentNode.Value);
                    var nodeToDelete = currentNode;
                    currentNode = currentNode.Next;
                    movedNodes[xPos, zPos].Remove(nodeToDelete);
                }
            }

            movedNodes[xPos, zPos].First.Value.IsVirtual = false;
        }
        //if C-O, oceanic subducts
        else
        {
            if (hasMultipleCo)
            {
                //not implemented
            }
            else if (hasMultipleOc)
            {
                //not implemented
            }
            else // just one O and one C
            {
                float subductedHeight;
                if (movedNodes[xPos, zPos].First.Value.Type == MaterialType.Oceanic) //O,C
                {
                    movedNodes[xPos, zPos].First.Value.IsVirtual = true;
                    movedNodes[xPos, zPos].First.Value.Height = movedNodes[xPos, zPos].First.Value.Height - subductionFactor; //subduct downwards
                    subductedHeight = movedNodes[xPos, zPos].First.Value.Height;

                    movedNodes[xPos, zPos].Last.Value.IsVirtual = false;
                    //move to the start of the list
                    movedNodes[xPos, zPos].AddFirst(movedNodes[xPos, zPos].Last.Value);
                    movedNodes[xPos, zPos].Remove(movedNodes[xPos, zPos].Last);
                }
                else //C,O
                {
                    movedNodes[xPos, zPos].First.Value.IsVirtual = false;

                    movedNodes[xPos, zPos].Last.Value.IsVirtual = true;
                    movedNodes[xPos, zPos].Last.Value.Height = movedNodes[xPos, zPos].Last.Value.Height - subductionFactor; //subduct downwards
                    subductedHeight = movedNodes[xPos, zPos].Last.Value.Height;
                }
            }
        }
    }



    private static void CheckSpaceAndUpdateDict(int x, int z, int width, int height, ref LinkedList<CrustNode>[,] movedNodes, ref Dictionary<Plate, int> plateCountsDict, ref bool found)
    {
        x = x % width;
        if (x < 0) { x = width + x; }
        z = z % height;
        if (z < 0) { z = height + z; }

        if (movedNodes[x, z].Count == 1 && movedNodes[x, z].First.Value.Plate != null)
        {
            if (plateCountsDict.ContainsKey(movedNodes[x, z].First.Value.Plate))
            {
                plateCountsDict[movedNodes[x, z].First.Value.Plate]++;
                found = true;
            }
        }
    }
}
