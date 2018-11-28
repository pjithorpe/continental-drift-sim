using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

using Random = UnityEngine.Random;
using Delaunay;

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
    private Node[,] nodes; // array of nodes for the whole mesh, arranged in a matrix according to their positions
    private List<Volcano> volcanos;


    // non-field definitions
    private float halfTriWidth;
    private int vertexCount;
    private int triCount;
    private Vector3[] verts;
    private int[] tris;
    private Color[] colors;
    private List<Node>[] movedNodes;

    /* Remove this when the temporary code in update mesh is removed --> */
    Plate p = new Plate(defaultHeight: 0.0f, xSpeed: 0, zSpeed: 0);

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
        this.volcanos = new List<Volcano>();

        this.halfTriWidth = triWidth / 2;

        /* Remove this when the temporary code in update mesh is removed --> */
        this.AddPlate(p);
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
            for (int i = 0; i < plates.Length; i++)
            {
                this.plates[i].Crust = this;
            }
        }
    }
    public List<Volcano> Volcanos
    {
        get { return this.volcanos; }
        set
        {
            this.volcanos = value;
            for (int i = 0; i < volcanos.Count; i++)
            {
                this.volcanos[i].Crust = this;
            }
        }
    }

    public void AddPlate(Plate p)
    {
        p.Crust = this;

        Plate[] newPlates = new Plate[plates.Length + 1];

        for (int i = 0; i < plates.Length; i++)
        {
            newPlates[i] = plates[i];
        }
        newPlates[plates.Length] = p;

        plates = newPlates;
    }

    public void AddVolcano(Volcano v)
    {
        v.Crust = this;
        volcanos.Add(v);
    }

    public void BuildMesh(bool addNoise)
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        vertexCount = width * height;
        triCount = (width - 1) * (height - 1) * 6;
        verts = new Vector3[vertexCount];
        nodes = new Node[width, height];
        tris = new int[triCount];

        //x and y (in number of triWidths/Lengths)
        int xPos;
        int zPos;

        //Precalculating floats which are used in loop
        float halfWidth = width / 20; // Have to add a small fraction for Mathf.PerlinNoise to work
        float seed = Random.Range(10, 100) + Random.Range(0.1f, 0.99f);
        //vertices
        if (addNoise)
        {
            for (int i = 0; i < verts.Length; i++)
            {
                xPos = i % width;
                zPos = i / width;

                float perlinNoise = Mathf.PerlinNoise(((xPos) / halfWidth) + seed, ((zPos) / halfWidth) + seed);

                float y = BaseHeight + (maxHeight * perlinNoise);

                if (zPos % 2 == 0)
                {
                    verts[i] = new Vector3(xPos * triWidth, y, zPos * triHeight);
                }
                else
                {
                    verts[i] = new Vector3((xPos * triWidth) + halfTriWidth, y, zPos * triHeight);
                }

                Node n = ObjectPooler.current.GetPooledNode();
                n.X = xPos;
                n.Z = zPos;
                n.Height = y;
                n.Density = 0.1f;
                nodes[xPos, zPos] = n;

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
                    verts[i] = new Vector3((xPos * triWidth) + halfTriWidth, 0, zPos * triHeight);
                }
                Node nd = ObjectPooler.current.GetPooledNode();
                nd.X = xPos;
                nd.Z = zPos;
                nd.Height = 0;
                nd.Density = 0.1f;
                nodes[xPos, zPos] = nd;

            }
        }

        mesh.vertices = verts;


        //triangles
        //vi = vertex index
        //ti = triangle index
        for (int ti = 0, vi = 0, y = 0; y < height - 1; y++, vi++)
        {
            for (int x = 0; x < width - 1; x++, ti += 6, vi++)
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
        for (int i = 0; i < plateCount; i++)
        {
            plates[i] = new Plate();
            plates[i].DefaultHeight = Random.Range(1.0f, 5.0f);
            this.AddPlate(plates[i]);

            //Add a random centroid to list TODO: Convert to make central centroids more likely (maybe a Gaussian?)
            centroids.Add(new Vector2(Random.Range(0, width * triWidth), Random.Range(0, height * triHeight)));
            nullColors.Add(0);
        }

        Voronoi voronoi = new Voronoi(centroids, nullColors, new Rect(0, 0, width * triWidth, height * triHeight));
        List<List<Vector2>> vorRegions = voronoi.Regions();

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
                            nodes[x, z - 1].Plate = plates[i];
                            fillPlotCount++;
                        }
                    }
                }
            }
        }
        //Do a final run over all nodes to make sure each one is assigned to a plate
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (nodes[j, i].Plate == null)
                {
                    if (nodes[(j + 1) % width, i].Plate != null) { nodes[j, i].Plate = nodes[(j + 1) % width, i].Plate; }
                    else if (nodes[j, (i + 1) % height].Plate != null) { nodes[j, i].Plate = nodes[j, (i + 1) % height].Plate; }
                    else if (nodes[Math.Abs(j - 1), i].Plate != null) { nodes[j, i].Plate = nodes[Math.Abs(j - 1), i].Plate; }
                    else if (nodes[j, Math.Abs(i - 1)].Plate != null) { nodes[j, i].Plate = nodes[j, Math.Abs(i - 1)].Plate; }
                }
            }
        }

        //temp
        Node[,] newNodes = new Node[width, height];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                //Get new x and y for prev node
                Node prevN = nodes[j, i];
                int newX = prevN.X;
                int newZ = prevN.Z;

                Node nd = ObjectPooler.current.GetPooledNode();
                nd.Copy(prevN);
                nd.X = newX;
                nd.Z = newZ;
                newNodes[newX, newZ] = nd;

                ObjectPooler.current.ReturnNodeToPool(prevN);


                int vertIndex = newX + (newZ * width);
                if (newZ % 2 == 0)
                {
                    verts[vertIndex] = new Vector3(newX * triWidth, newNodes[newX, newZ].Height, newZ * triHeight);
                }
                else
                {
                    verts[vertIndex] = new Vector3((newX * triWidth) + halfTriWidth, newNodes[newX, newZ].Height, newZ * triHeight);
                }

                float h = verts[vertIndex].y;
                float normalisedHeight = (h - baseHeight) / maxHeight;
                colors[vertIndex] = stage.PickColour(normalisedHeight, seaLevel);
            }
        }

        nodes = newNodes;

        mesh.vertices = verts;
        mesh.colors = colors;
        meshFilter.mesh = mesh;
        //end temp
    }

    // - takes a set of x,y coords and the heights to change them to
    // - set updateall to true to update all vertex colours
    public void UpdateMesh(float[] heights = null, bool updateAll = false)
    {
        Node[,] newNodes = new Node[width, height];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                //Get new x and y for prev node
                Node prevN = nodes[j, i];
                int dx = prevN.Plate.XSpeed;
                int dz = prevN.Plate.ZSpeed;
                int newX = (prevN.X + dx) % (width - 1);
                if (newX < 0) { newX = width + newX; }
                int newZ = (prevN.Z + dz) % (height - 1);
                if (newZ < 0) { newZ = height + newZ; }

                //Check if node is on a margin TEMPORARY
                int checkFromX = (j - dx) % (width - 1);
                int checkToX = (j + dx) % (width - 1);
                if (checkFromX < 0) { checkFromX = width + checkFromX; }
                if (checkToX < 0) { checkToX = width + checkToX; }
                int checkFromZ = (i - dz) % (height - 1);
                int checkToZ = (i + dz) % (height - 1);
                if (checkFromZ < 0) { checkFromZ = height + checkFromZ; }
                if (checkToZ < 0) { checkToZ = height + checkToZ; }

                if (nodes[checkToX, checkToZ].Plate != prevN.Plate)
                {
                    float differenceAngle = Angles.Similarity(dx, dz, nodes[checkToX, checkToZ].X, nodes[checkToX, checkToZ].Z);

                    if (differenceAngle > 0.75f * Mathf.PI)
                    {

                    }

                    Node nd = ObjectPooler.current.GetPooledNode();
                    nd.Copy(nodes[j, i]);
                    nd.X = j;
                    nd.Z = i;
                    if (newNodes[j, i] != null) // euuughghhh
                    {
                        ObjectPooler.current.ReturnNodeToPool(newNodes[j, i]);
                    }
                    newNodes[j, i] = nd;

                    //Random chance of island starting to be generated
                    /*float chance = Random.Range(0.0f, 1.0f);
                    if(chance>0.99f)
                    {
                        Volcano v = ObjectPooler.current.GetPooledVolcano();
                        v.X = j;
                        v.Z = i;
                        v.MaterialRate = Random.Range(0, 6); //How many rocks get thrown out of the volcano each frame
                        this.AddVolcano(v);
                    }*/
                }

                Node n = ObjectPooler.current.GetPooledNode();
                n.Copy(prevN);
                n.X = newX;
                n.Z = newZ;
                if (newNodes[newX, newZ] != null) // egughhhh
                {
                    ObjectPooler.current.ReturnNodeToPool(newNodes[newX, newZ]);
                }
                newNodes[newX, newZ] = n;


                int vertIndex = newX + (newZ * width);
                if (newZ % 2 == 0)
                {
                    verts[vertIndex] = new Vector3(newX * triWidth, newNodes[newX, newZ].Height, newZ * triHeight);
                }
                else
                {
                    verts[vertIndex] = new Vector3((newX * triWidth) + halfTriWidth, newNodes[newX, newZ].Height, newZ * triHeight);
                }

                float h = verts[vertIndex].y;
                float normalisedHeight = (h - baseHeight) / maxHeight;
                colors[vertIndex] = stage.PickColour(normalisedHeight, seaLevel);
            }
        }

        //release all the old nodes back to the pool
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                ObjectPooler.current.ReturnNodeToPool(nodes[j, i]);
            }
        }

        nodes = newNodes;

        // TEMPORARY! Do a final run over all nodes to make sure each on is assigned to a plate

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (nodes[j, i] == null)
                {
                    //EXTREMELY TEMPORARY FIX
                    nodes[j, i] = new Node(j, i);
                    nodes[j, i].Density = 0.1f;
                    nodes[j, i].Height = 100.0f;
                }
                if (nodes[j, i].Plate == null)
                {
                    nodes[j, i].Plate = p;
                }
            }
        }

        // end temp

        //Now run a particle desposition step for each volcano
        for (int i = 0; i < volcanos.Count; i++)
        {
            //Increase age
            volcanos[i].Age++;
            //If it's at the end of it's lifetime, remove it from the list and return it to the volcano object pool
            if (volcanos[i].Age >= 40)
            {
                ObjectPooler.current.ReturnVolcanoToPool(volcanos[i]);
                volcanos.RemoveAt(i);
            }
        }

        mesh.vertices = verts;
        mesh.colors = colors;
        meshFilter.mesh = mesh;
    }
}