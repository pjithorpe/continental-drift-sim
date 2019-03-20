using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class PlateGeneration
{
    private static List<List<Vector2>> GenerateCylindricalVoronoi(List<Vector2> centroids, float width, float height)
    {
        var nullColors = new List<uint>(); //needed to call Voronoi(), but redundant in this use case

        int noOfCentroids = centroids.Count;
        for (int i = 0; i < noOfCentroids; i++)
        {
            //We copy all of the centroids to create 2 "ghost" versions of the voronoi which we we stitch onto either side of the real one. This will help us to cylindricalise the map.
            Vector2 ghostCentroidRight = new Vector2(centroids[i].x + width, centroids[i].y);
            Vector2 ghostCentroidRightRight = new Vector2(centroids[i].x + (2f * width), centroids[i].y);
            centroids.Add(ghostCentroidRight);
            centroids.Add(ghostCentroidRightRight);

            //the colors list has to be the same size as the centroids list
            nullColors.Add(0);
            nullColors.Add(0);
            nullColors.Add(0);
        }

        Delaunay.Voronoi voronoi = new Delaunay.Voronoi(centroids, nullColors, new Rect(0, 0, 3f * width, height));
        List<List<Vector2>> vorRegions = voronoi.Regions();
        var centralVorRegions = new List<List<Vector2>>();

        //Now run the cylindricalisation step by removing all regions that are solely in the 2 ghost diagrams
        for (int i = 0; i < vorRegions.Count; i++)
        {
            bool hasPointInMiddle = false;
            for (int j = 0; j < vorRegions[i].Count; j++)
            {
                if ((vorRegions[i][j].x >= width) && (vorRegions[i][j].x < (2f * width)))
                {
                    hasPointInMiddle = true;
                    break;
                }
            }

            if (hasPointInMiddle)
            {
                centralVorRegions.Add(vorRegions[i]); // this region has points within our final diagram, so keep it
            }
        }

        var cylindricalVorRegions = new List<List<Vector2>>();
        //Also remove any overlap regions on the left hand side so that we only have one copy of each wrap-around polygon (on the right hand side)
        for (int i = 0; i < centralVorRegions.Count; i++)
        {
            bool hasPointOutsideLeftBoundary = false;
            for (int j = 0; j < centralVorRegions[i].Count; j++)
            {
                if (centralVorRegions[i][j].x < width)
                {
                    hasPointOutsideLeftBoundary = true;
                    break;
                }
            }

            if (!hasPointOutsideLeftBoundary)
            {
                // this region does not spill over the left side, so keep it, and shift all its points to the left by a mesh width
                for (int p = 0; p < centralVorRegions[i].Count; p++)
                {
                    centralVorRegions[i][p] = new Vector2(centralVorRegions[i][p].x - width, centralVorRegions[i][p].y);
                }
                cylindricalVorRegions.Add(centralVorRegions[i]);
            }
        }

        return cylindricalVorRegions;
    }

    public static List<CrustNode>[,] FillPlates(List<List<Vector2>> vorRegions, Plate[] plates, List<CrustNode>[,] nodes)
    {
        int nodesWidth = nodes.GetLength(0);
        int nodesHeight = nodes.GetLength(1);

        //Now that we have the regions, we can use a filling algorithm to assign all the vertices in each polygon to a plate
        for (int i = 0; i < vorRegions.Count; i++)
        {
            //get the min/max values for the region
            float minX, maxX, minZ, maxZ;
            minX = maxX = vorRegions[i][0].x;
            minZ = maxZ = vorRegions[i][0].y;

            int j = 0;
            for (j = 1; j < vorRegions[i].Count; j++)
            {
                if (vorRegions[i][j].x < minX)
                {
                    minX = vorRegions[i][j].x;
                }
                else if (vorRegions[i][j].x > maxX)
                {
                    maxX = vorRegions[i][j].x;
                }

                if (vorRegions[i][j].y < minZ)
                {
                    minZ = vorRegions[i][j].y;
                }
                else if (vorRegions[i][j].y > maxZ)
                {
                    maxZ = vorRegions[i][j].y;
                }
            }


            //Now fill it in
            int x1 = (int)Math.Round(minX, 0);
            int x2 = (int)Math.Round(maxX, 0);
            int z1 = (int)Math.Round(minZ, 0);
            int z2 = (int)Math.Round(maxZ, 0);

            //Debug.Log(x1.ToString() + " " + x2.ToString() + " " + z1.ToString() + " " + z2.ToString() + " ");

            int fillPlotCount = 0;
            int polyCorners = vorRegions[i].Count;
            int[] markers = new int[z2 - z1];
            int n;

            for (int z = z1; z <= z2; z++)
            {
                int markerCount = 0;
                n = polyCorners - 1;

                j = 0;
                for (j = 0; j < polyCorners; j++)
                {
                    if (vorRegions[i][j].y < z && vorRegions[i][n].y >= z || vorRegions[i][n].y < z && vorRegions[i][j].y >= z)
                    {
                        markers[markerCount++] = (int)(vorRegions[i][j].x + (z - vorRegions[i][j].y) / (vorRegions[i][n].y - vorRegions[i][j].y) * (vorRegions[i][n].x - vorRegions[i][j].x));
                    }
                    n = j;
                }

                j = 0;
                while (j < markerCount - 1)
                {
                    if (markers[j] > markers[j + 1])
                    {
                        int swap = markers[j];
                        markers[j] = markers[j + 1];
                        markers[j + 1] = swap;

                        if (j != 0) { j--; }
                    }
                    else
                    {
                        j++;
                    }
                }

                for (j = 0; j < markerCount; j += 2)
                {
                    if (markers[j] >= x2) break;
                    if (markers[j + 1] > x1)
                    {
                        if (markers[j] < x1) markers[j] = x1;
                        if (markers[j + 1] > x2) markers[j + 1] = x2;
                        for (int x = markers[j]; x < markers[j + 1]; x++)
                        {
                            //have to make sure the x value is wrapped around if needed because of the cylindricalisation
                            int wrappedX = x % nodesWidth;
                            nodes[wrappedX, z - 1][0].Plate = plates[i];
                            fillPlotCount++;
                        }
                    }
                }
            }
        }

        //Do a final run over all nodes to make sure each one is assigned to a plate
        for (int i = 0; i < nodesHeight; i++)
        {
            for (int j = 0; j < nodesWidth; j++)
            {
                if (nodes[j, i][0].Plate == null)
                {
                    if (nodes[(j + 1) % nodesWidth, i][0].Plate != null) { nodes[j, i][0].Plate = nodes[(j + 1) % nodesWidth, i][0].Plate; }
                    else if (nodes[j, (i + 1) % nodesHeight][0].Plate != null) { nodes[j, i][0].Plate = nodes[j, (i + 1) % nodesHeight][0].Plate; }
                    else if (nodes[Math.Abs(j - 1), i][0].Plate != null) { nodes[j, i][0].Plate = nodes[Math.Abs(j - 1), i][0].Plate; }
                    else if (nodes[j, Math.Abs(i - 1)][0].Plate != null) { nodes[j, i][0].Plate = nodes[j, Math.Abs(i - 1)][0].Plate; }
                }
            }
        }

        return nodes;
    }


    /*
    * Generates a random set of thin plates as an initial state
    */
    public static Crust SplitCrustIntoPlates(Crust crust, float width, float height, int plateCount, int voronoiRelaxationSteps)
    {
        var plates = new Plate[plateCount];
        var centroids = new List<Vector2>();

        // First, choose points on the mesh at random as our plate centres (centroids)
        for (int i = 0; i < plateCount; i++)
        {
            plates[i] = new Plate();
            plates[i].DefaultHeight = Random.Range(1.0f, 5.0f);
            plates[i].Density = Random.Range(0.4f, 1.0f);
            /*this.AddPlate(plates[i]); **************************************************************************************************CRUST*/

            //Add a random centroid to list TODO: Convert to make central centroids more likely (maybe a Gaussian?)
            Vector2 centroid = new Vector2(Random.Range(0, width), Random.Range(0, height));
            centroids.Add(centroid);
        }

        //Now generate the voronoi (multiple times if we perform relaxations)
        List<List<Vector2>> cylindricalVorRegions = GenerateCylindricalVoronoi(centroids, width, height);

        //TODO: relaxation currently bugged (number of centroids goes down every time relaxation is run)
        if (voronoiRelaxationSteps > 0)
        {
            for (int i = 0; i < voronoiRelaxationSteps; i++)
            {
                //relaxation
                var relaxedCentroids = new List<Vector2>();
                for (int r = 0; r < cylindricalVorRegions.Count; r++)
                {
                    float totalX = 0f;
                    float totalY = 0f;
                    for (int p = 0; p < cylindricalVorRegions[r].Count; p++)
                    {
                        totalX += cylindricalVorRegions[r][p].x;
                        totalY += cylindricalVorRegions[r][p].y;
                    }

                    float averageX = totalX / cylindricalVorRegions[r].Count;
                    float averageY = totalY / cylindricalVorRegions[r].Count;

                    relaxedCentroids.Add(new Vector2(averageX, averageY));
                }

                //re-generate
                cylindricalVorRegions = GenerateCylindricalVoronoi(relaxedCentroids, width, height);
            }
        }


        Debug.Log("Number of plates: " + plateCount.ToString() + ", Number of regions: " + cylindricalVorRegions.Count.ToString());

        crust.CrustNodes = FillPlates(cylindricalVorRegions, plates, crust.CrustNodes);
        crust.Plates = plates;
        
        for (int i = 0; i < plateCount; i++)
        {
            plates[i].RecalculateMass();
            // Debug.Log("nodes: " + plates[i].NodeCount.ToString() + "density: " + plates[i].Density.ToString() + "mass: " + plates[i].Mass.ToString());
        }


        /*var newNodes = new List<CrustNode>[crust.Width, crust.Height];

        for (int i = 0; i < crust.Height; i++)
        {
            for (int j = 0; j < crust.Width; j++)
            {
                for (int n_i = 0; n_i < crust.CrustNodes[j, i].Count; n_i++)
                {
                    //Get new x and y for prev node
                    CrustNode prevN = crust.CrustNodes[j, i][0];
                    int newX = prevN.X;
                    int newZ = prevN.Z;

                    CrustNode nd = ObjectPooler.current.GetPooledNode();
                    nd.Copy(prevN);
                    nd.X = newX;
                    nd.Z = newZ;
                    newNodes[newX, newZ] = new List<CrustNode>();
                    newNodes[newX, newZ].Add(nd);

                    ObjectPooler.current.ReturnNodeToPool(prevN);


                    int vertIndex = newX + (newZ * crust.Width);
                    if (newZ % 2 == 0)
                    {
                        verts[vertIndex].x = newX * triWidth;
                        verts[vertIndex].y = newNodes[newX, newZ][0].Height;
                        verts[vertIndex].z = newZ * triHeight;
                    }
                    else
                    {
                        verts[vertIndex].x = (newX * triWidth) + halfTriWidth;
                        verts[vertIndex].y = newNodes[newX, newZ][0].Height;
                        verts[vertIndex].z = newZ * triHeight;
                    }

                    float h = verts[vertIndex].y;
                    float normalisedHeight = h / maxHeight;
                    colors[vertIndex] = stage.PickColour(normalisedHeight, seaLevel);
                }
            }
        }

        
        crust.CrustNodes = newNodes;
        /*
        mesh.vertices = verts;
        mesh.colors = colors;
        meshFilter.mesh = mesh;
        */

        return crust;
    }
}
