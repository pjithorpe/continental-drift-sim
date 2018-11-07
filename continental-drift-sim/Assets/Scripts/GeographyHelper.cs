using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

using Delaunay;

using ColorExtended;

namespace GeographyHelper
{
    public class Crust
    {
        // field private vars
        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private int width;
        private int height;
        private float triWidth;
        private float triHeight;
        private float baseHeight;
        private float maxHeight; //distance above base height, not total max height
        private float seaLevel; // between 0.0 and 1.0 representing positioning between baseHeight and maxHeight
        private Stage stage;
        private Plate[] plates;


        // non-field definitions
        private int vertexCount;
        private int triCount;
        private Vector3[] verts;
        private int[] tris;
        private Color[] colors;

        // Constructor
        public Crust(MeshFilter mf, MeshRenderer mr, int width = 256, int height = 256, float triWidth = 1.0f, float triHeight = 1.0f, Mesh mesh = null, float baseHeight = 10.0f, float maxHeight = 20.0f, float seaLevel = 0.0f, Stage stage = null, Plate[] plates = null)
        {
            this.width = width;
            this.height = height;
            this.triWidth = triWidth;
            this.triHeight = triHeight;
            if (mesh == null) { this.mesh = new Mesh(); }
            else { this.mesh = mesh; }
            this.meshFilter = mf;
            this.meshRenderer = mr;
            this.baseHeight = baseHeight;
            this.maxHeight = maxHeight;
            this.seaLevel = seaLevel;
            if (stage == null) { this.stage = new CoolingStage(); }
            if (plates == null) { this.plates = new Plate[0]; }
            else { this.plates = plates; }
        }

        public Mesh Mesh
        {
            get { return this.mesh; }
            set
            {
                this.mesh = value;
                if (this.mesh.vertices != null && this.mesh.vertices.Length > 0)
                {
                    verts = this.mesh.vertices;
                }
            }
        }
        public MeshFilter MeshFilter
        {
            get { return this.meshFilter; }
            set { this.meshFilter = value; }
        }
        public MeshRenderer MeshRenderer
        {
            get { return this.meshRenderer; }
            set { this.meshRenderer = value; }
        }
        public int Width
        {
            get { return this.width; }
            set { this.width = value; }
        }
        public int Height
        {
            get { return this.height; }
            set { this.height = value; }
        }
        public float BaseHeight
        {
            get { return this.baseHeight; }
            set { this.baseHeight = value; }
        }
        public float MaxHeight
        {
            get { return this.maxHeight; }
            set { this.maxHeight = value; }
        }
        public float SeaLevel
        {
            get { return this.seaLevel; }
            set { this.seaLevel = value; }
        }
        public Stage Stage
        {
            get { return this.stage; }
            set { this.stage = value; }
        }
        public Plate[] Plates
        {
            get { return this.plates; }
            set
            {
                this.plates = value;
                for (int i=0; i<plates.Length; i++)
                {
                    this.plates[i].Crust = this;
                }
            }
        }

        public void AddPlate(Plate p)
        {
            p.Crust = this;

            Plate[] newPlates = new Plate[plates.Length + 1];

            for (int i=0; i<plates.Length; i++)
            {
                newPlates[i] = plates[i];
            }
            newPlates[plates.Length] = p;

            plates = newPlates;
        }

        public void BuildMesh(bool addNoise)
        {
            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            vertexCount = width * height;
            triCount = (width - 1) * (height -1) * 6;
            verts = new Vector3[vertexCount];
            tris = new int[triCount];

            //x and y (in number of triWidths/Lengths)
            int xPos;
            int zPos;

            //Precalculating floats which are used in loop
            float halfTriWidth = triWidth / 2;
            float halfWidth = width / 20; // Have to add a small fraction for Mathf.PerlinNoise to work
            float seed = Random.Range(10, 100) + Random.Range(0.1f, 0.99f);
            //vertices
            if (addNoise)
            {
                for (int i = 0; i < verts.Length; i++)
                {
                    xPos = i % width;
                    zPos = i / width;

                    float perlinNoise = Mathf.PerlinNoise(((i % width) / halfWidth) + seed, ((i / width) / halfWidth) + seed);

                    float y = BaseHeight + (maxHeight * perlinNoise);

                    if (zPos % 2 == 0)
                    {
                        verts[i] = new Vector3(xPos * triWidth, y, zPos * triHeight);
                    }
                    else
                    {
                        verts[i] = new Vector3((xPos * triWidth) + halfTriWidth, y, zPos * triHeight);
                    }

                }
            }
            else
            {
                for (int i = 0; i < verts.Length; i++)
                {

                    xPos = i % width;
                    zPos = i / width;

                    if (zPos % 2 == 0)
                    {
                        verts[i] = new Vector3(xPos * triWidth, 0, zPos * triHeight);
                    }
                    else
                    {
                        verts[i] = new Vector3((xPos * triWidth) + (triWidth / 2), 0, zPos * triHeight);
                    }

                }
            }

            mesh.vertices = verts;


            //triangles
            //vi = vertex index
            //ti = triangle index
            for (int ti=0, vi=0, y=0; y<height-1; y++, vi++)
            {
                for (int x = 0; x < width-1; x++, ti+=6, vi++)
                {
                    if ((vi / width) % 2 == 0)
                    {
                        tris[ti] = vi;
                        tris[ti + 3] = tris[ti + 2] = vi + 1;
                        tris[ti + 4] = tris[ti + 1] = vi + width;
                        tris[ti + 5] = vi + width + 1;
                    }
                    else
                    {
                        tris[ti] = tris[ti + 3] = vi;
                        tris[ti + 1] = vi + width;
                        tris[ti + 2] = tris[ti + 4] = vi + width + 1;
                        tris[ti + 5] = vi + 1;
                    }
                }
            }
            mesh.triangles = tris;


            //colors
            var oceanBlue = new Color32(28, 107, 160, 255);
            var bedrockGrey = new Color32(79, 70, 60, 255);
            var mountainGrey = new Color32(140, 127, 112, 255);
            var sandBrown = new Color32(100, 105, 64, 255);

            //currently just assumes magma stage
            colors = new Color[verts.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                float normalisedHeight = (verts[i].y - baseHeight) / maxHeight;
                colors[i] = stage.PickColour(normalisedHeight, seaLevel);
            }

            mesh.colors = colors;

            mesh.RecalculateNormals();

            
            meshFilter.mesh = mesh;
            
            meshRenderer.material = Resources.Load("Materials/TestMaterial", typeof(Material)) as Material;

            Camera mainCam = Camera.main;
            mainCam.enabled = true;
            mainCam.aspect = 1;
            mainCam.transform.position = new Vector3(width * triWidth * 0.5f, 50.0f, height * triHeight * 0.5f);
            //This enables the orthographic mode
            mainCam.orthographic = true;
            //Set the size of the viewing volume you'd like the orthographic Camera to pick up (5)
            mainCam.orthographicSize = width * triWidth * 0.5f;
            //Set the orthographic Camera Viewport size and position
            mainCam.rect = new Rect(0.0f, 0.0f, width * triWidth, height * triHeight);


            /*Vector3[] normals = new Vector3[tris.Length];

            for (int i = 0; i < normals.Length; i++)
            {
                normals[i] = Vector3.up;
            }

            mesh.normals = normals;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            */
            /*Vector2[] uv = new Vector2[vertexCount];

            uv[0] = new Vector2(0, 0);
            uv[1] = new Vector2(1, 0);
            uv[2] = new Vector2(0, 1);
            uv[3] = new Vector2(1, 1);

            mesh.uv = uv;*/
        }

        /*
         * Generates a random set of thin plates as an initial state
         */ 
        public void InitialiseCrust(int plateCount)
        {
            var plates = new Plate[plateCount];
            var centroids = new List<Vector2>();
            var nullColors = new List<uint>(); //needed to call Voronoi(), but redundant in this use case

            // First, choose points on the mesh at random as our plate centres (centroids)
            for (int i=0; i<plateCount; i++)
            {
                plates[i] = new Plate();
                this.AddPlate(plates[i]);

                //Add a random centroid to list TODO: Convert to make central centroids more likely (maybe a Gaussian?)
                centroids.Add(new Vector2(Random.Range(0, width * triWidth), Random.Range(0, height * triHeight)));
                nullColors.Add(0); 
            }

            Voronoi voronoi = new Voronoi(centroids, nullColors, new Rect(0, 0, width * triWidth, height * triHeight));
            List<List<Vector2>> vorRegions = voronoi.Regions();

            //Now that we have the regions, we can use a filling algorithm to assign all the vertices in each polygon to a plate
            for (int i=0; i<vorRegions.Count; i++)
            {
                //get the min/max values for the region
                float minX, maxX, minZ, maxZ;
                minX = maxX = vorRegions[i][0].x;
                minZ = maxZ = vorRegions[i][0].y;

                int j = 0;
                for (j=1; j<vorRegions[i].Count; j++)
                {
                    if(vorRegions[i][j].x < minX)
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

                int[,] fillPlot = new int[(x2 - x1) * (z2 - z1), 2];
                int fillPlotCount = 0;
                int polyCorners = vorRegions[i].Count;
                int[] nodes = new int[z2 - z1];
                int n;

                for (int z = z1; z <= z2; z++)
                {
                    int nodeCount = 0;
                    n = polyCorners - 1;

                    j = 0;
                    for (j = 0; j < polyCorners; j++)
                    {
                        if (vorRegions[i][j].y < z && vorRegions[i][n].y >= z || vorRegions[i][n].y < z && vorRegions[i][j].y >= z)
                        {
                            nodes[nodeCount++] = (int)(vorRegions[i][j].x + (z - vorRegions[i][j].y) / (vorRegions[i][n].y - vorRegions[i][j].y) * (vorRegions[i][n].x - vorRegions[i][j].x));
                        }
                        n = j;
                    }

                    j = 0;
                    while (j < nodeCount - 1)
                    {
                        if (nodes[j] > nodes[j + 1])
                        {
                            int swap = nodes[j];
                            nodes[j] = nodes[j + 1];
                            nodes[j + 1] = swap;

                            if (j != 0) { j--; }
                        }
                        else
                        {
                            j++;
                        }
                    }

                    for (j = 0; j < nodeCount; j += 2)
                    {
                        if (nodes[j] >= x2) break;
                        if (nodes[j + 1] > x1)
                        {
                            if (nodes[j] < x1) nodes[j] = x1;
                            if (nodes[j + 1] > x2) nodes[j + 1] = x2;
                            for (int x = nodes[j]; x < nodes[j + 1]; x++)
                            {
                                fillPlot[fillPlotCount, 0] = x;
                                fillPlot[fillPlotCount, 1] = z;
                                fillPlotCount++;
                            }
                        }
                    }
                }

                //make plate remember its plot so we don't have to recalc if we want to use it later
                plates[i].VertexPlot = fillPlot;
                plates[i].DrawPlate();
            }
        }

        // - takes a set of x,y coords and the heights to change them to
        // - set updateall to true to update all vertex colours
        public void UpdateMesh(int[,] changes = null, float[] heights = null, bool updateAll = false)
        {
            if (updateAll)
            {
                for (int i=0; i<verts.Length; i++)
                {
                    float h = verts[i].y;
                    float normalisedHeight = (h - baseHeight) / maxHeight;
                    colors[i] = stage.PickColour(normalisedHeight, seaLevel);
                }
            }
            else
            {
                if (changes.GetLength(0) != heights.Length)
                {
                    //Error!
                    Debug.Log("changes and heights arrays are different sizes!");
                }

                for (int i=0; i<changes.GetLength(0); i++)
                {
                    //Debug.Log("i: " + i.ToString() + " , xPos: " + changes[i, 0] + " , yPos: " + changes[i, 1]);
                    int xPos = changes[i, 0];
                    int yPos = changes[i, 1];
                    int vertIndex = yPos * width + xPos;
                    //Debug.Log(xPos.ToString() + " " + yPos.ToString() + " " + vertIndex.ToString() + " / " + verts.Length.ToString());

                    float h = heights[i];

                    //This if statement is a bit of a temp fix (there is some issue with points being 1 more than they should be on the y axis)
                    if (vertIndex < verts.Length)
                    {
                        verts[vertIndex] = new Vector3(verts[vertIndex].x, h, verts[vertIndex].z);
                        float normalisedHeight = (h - baseHeight) / maxHeight;
                        colors[vertIndex] = stage.PickColour(normalisedHeight, seaLevel);
                    }
                }
            }
            

            mesh.vertices = verts;
            mesh.colors = colors;
            meshFilter.mesh = mesh;
        }
    }

    public class Plate
    {
        // field private vars
        private int[,] vertexPlot;
        private float defaultHeight = 5.0f;
        private float xSpeed = 0.0f;
        private float zSpeed = 0.0f;
        private Crust crust;

        // non-field definitions
        //not (get/set)able
        private float minX, maxX, minZ, maxZ;

        public Plate(int[,] vertexPlot = null, float defaultHeight = 5.0f, float xSpeed = 0.0f, float zSpeed = 0.0f, Crust crust = null)
        {
            this.vertexPlot = vertexPlot;
            this.defaultHeight = defaultHeight;
            this.xSpeed = xSpeed;
            this.zSpeed = zSpeed;
            if (crust != null) { crust.AddPlate(this); }

            this.SetBoundaries();
        }

        
        public int[,] VertexPlot
        {
            get { return this.vertexPlot; }
            set { this.vertexPlot = value;  }
        }
        public float DefaultHeight //default height of vertices inside the plate
        {
            get { return this.defaultHeight; }
            set { this.defaultHeight = value; }
        }
        public float XSpeed
        {
            get { return this.xSpeed; }
            set { this.xSpeed = value; }
        }
        public float ZSpeed
        {
            get { return this.zSpeed; }
            set { this.zSpeed = value; }
        }
        public Crust Crust
        {
            get { return this.crust; }
            set { this.crust = value; }
        }
        
        private void SetBoundaries()
        {
            /*if (outline != null)
            {
                minX = maxX = outline[0].x;
                minZ = maxZ = outline[0].y;

                for (int i = 1; i < outline.Length; i++)
                {
                    float pX = outline[i].x;
                    float pZ = outline[i].y;

                    if (pX < minX) { minX = pX; }
                    else if (pX > maxX) { maxX = pX; }
                    if (pZ < minZ) { minZ = pZ; }
                    else if (pZ > maxZ) { maxZ = pZ; }
                }

                Debug.Log(minX.ToString() + " " + maxX.ToString() + " " + minZ.ToString() + " " + maxZ.ToString() + " ");
            }*/
        }


        public void DrawPlate()
        {
            int[,] plot;
            //outline
            if (this.vertexPlot != null)
            {
                plot = this.vertexPlot;
            }
            else
            {
                Debug.Log("No plot or outline for plate. Cancelling DrawPlate().");
                return;
            }


            var heights = new float[plot.GetLength(0)];

            //temp
            for (int i = 0; i < heights.Length; i++)
            {
                heights[i] = defaultHeight;
            }

            crust.UpdateMesh(plot, heights);
        }

        public void MovePlate()
        {
            int[,] prevPlot;
            if (vertexPlot != null)
            {
                prevPlot = vertexPlot;
            }
            else
            {
                Debug.Log("No plot or outline for plate. Cancelling MovePlate().");
                return;
            }

            //Reset previous plate vertices
            var heights = new float[prevPlot.GetLength(0)];
            for (int i=0; i<heights.Length; i++)
            {
                heights[i] = crust.BaseHeight;
            }

            crust.UpdateMesh(vertexPlot, heights);

            //Now draw plate in new posistion

            //TO DO !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            minX += xSpeed;
            maxX += xSpeed;
            minZ += zSpeed;
            maxZ += zSpeed;

            //temp
            heights = new float[vertexPlot.GetLength(0)];
            for (int i=0; i<heights.Length; i++)
            {
                heights[i] = this.DefaultHeight;
            }

            crust.UpdateMesh(vertexPlot, heights);
        }
    }



    /*********
     * STAGES
     *********/

    public interface Stage
    {
        Color PickColour(float normailsedHeight, float seaLevel);
    }

    public class CoolingStage : Stage
    {
        public Color PickColour(float normalisedHeight, float seaLevel)
        {
            if (normalisedHeight <= seaLevel)
            {
                return Color.Lerp(Color.yellow, Color.red, normalisedHeight / seaLevel);
            }
            else if (seaLevel < normalisedHeight && normalisedHeight < seaLevel + 0.2f)
            {
                return ColorEx.bedrockGrey;
            }
            else
            {
                return Color.Lerp(ColorEx.bedrockGrey, ColorEx.mountainGrey, (normalisedHeight - (seaLevel + 0.2f)) / (1 - (seaLevel + 0.2f)));
            }
        }
    }

    public class WaterStage: Stage
    {
        public Color PickColour(float normalisedHeight, float seaLevel)
        {
            return Color.Lerp(ColorEx.bedrockGrey, ColorEx.mountainGrey, normalisedHeight);
        }
    } 

    public class LifeStage : Stage
    {
        public Color PickColour(float normalisedHeight, float seaLevel)
        {
            if (normalisedHeight <= 0.45f)
            {
                return Color.Lerp(ColorEx.bedrockGrey, ColorEx.mountainGrey, normalisedHeight / 0.45f);
            }
            if ((0.45f < normalisedHeight) && (normalisedHeight < 0.55f))
            {
                return ColorEx.sandBrown;
            }
            else
            {
                return Color.Lerp(ColorEx.bedrockGrey, Color.green, normalisedHeight);
            }
        }
    }





    public class OceanicPlate : Plate
    {

    }

    public class ContinentalPlate : Plate
    {

    }
}
