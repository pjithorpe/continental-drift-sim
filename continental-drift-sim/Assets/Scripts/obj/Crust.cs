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
    private List<CrustNode>[,] crustNodes; // array of nodes for the whole mesh, arranged in a matrix according to their positions
    private List<Volcano> stratoVolcanos;
    private List<Volcano> shieldVolcanos;


    // non-field definitions
    private float halfTriWidth;
    private int vertexCount;
    private int triCount;
    private Vector3[] verts;
    private int[] tris;
    private Color[] colors;
    private LinkedList<CrustNode>[,] movedCrustNodes;

    //volcanos
    float rockSize;
    float heightSimilarityEpsilon; //in the particle deposition algoorithm, if heights are this distance away from eachother, they will be considered equal

    /* Remove this when the temporary code in update mesh is removed --> */
    Plate p = new Plate(defaultHeight: 0.0f, xSpeed: 0, zSpeed: 0);

    // Constructor
    public Crust(MeshFilter mf, MeshRenderer mr, int width = 256, int height = 256, float triWidth = 1.0f, float triHeight = 1.0f, Mesh mesh = null, float baseHeight = 10.0f, float maxHeight = 5.0f, float seaLevel = 0.0f, Stage stage = null, Plate[] plates = null)
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
        this.shieldVolcanos = new List<Volcano>();
        this.stratoVolcanos = new List<Volcano>();

        this.halfTriWidth = triWidth / 2;

        this.rockSize = maxHeight / 10f;
        this.heightSimilarityEpsilon = rockSize * 0.2f;

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
    public List<Volcano> ShieldVolcanos
    {
        get { return this.shieldVolcanos; }
        set { this.shieldVolcanos = value; }
    }
    public List<Volcano> StratoVolcanos
    {
        get { return this.stratoVolcanos; }
        set { this.stratoVolcanos = value; }
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

    public void AddShieldVolcano(Volcano v)
    {
        shieldVolcanos.Add(v);
    }
    public void AddStratoVolcano(Volcano v)
    {
        stratoVolcanos.Add(v);
    }

    public void BuildMesh(bool addNoise)
    {
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        vertexCount = width * height;
        triCount = (width - 1) * (height - 1) * 6;
        verts = new Vector3[vertexCount];
        crustNodes = new List<CrustNode>[width, height];
        movedCrustNodes = new LinkedList<CrustNode>[width, height];
        tris = new int[triCount];

        //x and y (in number of triWidths/Lengths)
        int xPos;
        int zPos;

        //Precalculating floats which are used in loop
        float perlinFraction = width / 20; // Have to add a small fraction for Mathf.PerlinNoise to work
        float offset = Random.Range(10, 100) + Random.Range(0.1f, 0.99f);
        //vertices
        if (addNoise)
        {
            for (int i = 0; i < verts.Length; i++)
            {
                xPos = i % width;
                zPos = i / width;

                float perlinNoise = Mathf.PerlinNoise(((xPos) / perlinFraction) + offset, ((zPos) / perlinFraction) + offset);

                float y = BaseHeight + (maxHeight * perlinNoise);

                if (zPos % 2 == 0)
                {
                    verts[i] = new Vector3(xPos * triWidth, y, zPos * triHeight);
                }
                else
                {
                    verts[i] = new Vector3((xPos * triWidth) + halfTriWidth, y, zPos * triHeight);
                }

                CrustNode n = ObjectPooler.current.GetPooledNode();
                n.X = xPos;
                n.Z = zPos;
                n.Height = y;
                n.Density = 0.1f;
                n.IsVirtual = false;
                crustNodes[xPos, zPos] = new List<CrustNode>();
                crustNodes[xPos, zPos].Add(n);
                movedCrustNodes[xPos, zPos] = new LinkedList<CrustNode>();

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
                CrustNode nd = ObjectPooler.current.GetPooledNode();
                nd.X = xPos;
                nd.Z = zPos;
                nd.Height = 0;
                nd.Density = 0.1f;
                nd.IsVirtual = false;
                crustNodes[xPos, zPos] = new List<CrustNode>();
                crustNodes[xPos, zPos].Add(nd);
                movedCrustNodes[xPos, zPos] = new LinkedList<CrustNode>();
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


        // Now draw the UI and set the camera dimensions


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
            plates[i].Density = Random.Range(0.0f, 1.0f);
            this.AddPlate(plates[i]);

            //Add a random centroid to list TODO: Convert to make central centroids more likely (maybe a Gaussian?)
            centroids.Add(new Vector2(Random.Range(0, width * triWidth), Random.Range(0, height * triHeight)));
            nullColors.Add(0);
        }

        Voronoi voronoi = new Voronoi(centroids, nullColors, new Rect(0, 0, width * triWidth, height * triHeight));
        List<List<Vector2>> vorRegions = voronoi.Regions();
        Debug.Log("Number of plates: " + plateCount.ToString() + ", Number of regions: " + vorRegions.Count.ToString());

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
                            crustNodes[x, z - 1][0].Plate = plates[i];
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
                if (crustNodes[j, i][0].Plate == null)
                {
                    if (crustNodes[(j + 1) % width, i][0].Plate != null) { crustNodes[j, i][0].Plate = crustNodes[(j + 1) % width, i][0].Plate; }
                    else if (crustNodes[j, (i + 1) % height][0].Plate != null) { crustNodes[j, i][0].Plate = crustNodes[j, (i + 1) % height][0].Plate; }
                    else if (crustNodes[Math.Abs(j - 1), i][0].Plate != null) { crustNodes[j, i][0].Plate = crustNodes[Math.Abs(j - 1), i][0].Plate; }
                    else if (crustNodes[j, Math.Abs(i - 1)][0].Plate != null) { crustNodes[j, i][0].Plate = crustNodes[j, Math.Abs(i - 1)][0].Plate; }
                }
            }
        }

        //temp
        var newNodes = new List<CrustNode>[width, height];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                for (int n_i = 0; n_i<crustNodes[j,i].Count; n_i++)
                {
                    //Get new x and y for prev node
                    CrustNode prevN = crustNodes[j, i][0];
                    int newX = prevN.X;
                    int newZ = prevN.Z;

                    CrustNode nd = ObjectPooler.current.GetPooledNode();
                    nd.Copy(prevN);
                    nd.X = newX;
                    nd.Z = newZ;
                    newNodes[newX, newZ] = new List<CrustNode>();
                    newNodes[newX, newZ].Add(nd);

                    ObjectPooler.current.ReturnNodeToPool(prevN);


                    int vertIndex = newX + (newZ * width);
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
                    float normalisedHeight = (h - baseHeight) / maxHeight;
                    colors[vertIndex] = stage.PickColour(normalisedHeight, seaLevel);
                }
            }
        }

        crustNodes = newNodes;

        mesh.vertices = verts;
        mesh.colors = colors;
        meshFilter.mesh = mesh;
        //end temp
    }




    PlateType[] types = new PlateType[5];
    Dictionary<Plate, int> singlePlateSpacesCounts = new Dictionary<Plate, int>();
    public void UpdateMesh()
    {
        MoveNodes(); // for every node, move it based on its plate's speed

        //debug
        /*
        int emptyMovedNodesCount = 0;
        int singleMovedNodesCount = 0;
        int multipleMovedNodesCount = 0;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (movedCrustNodes[j,i].Count == 0)
                {
                    emptyMovedNodesCount++;
                }
                else if (movedCrustNodes[j,i].Count == 1)
                {
                    singleMovedNodesCount++;
                }
                else
                {
                    multipleMovedNodesCount++;
                }
            }
        }
        Debug.Log("Empty moved nodes count: " + emptyMovedNodesCount.ToString());
        Debug.Log("Single moved nodes count: " + singleMovedNodesCount.ToString());
        Debug.Log("Multiple moved nodes count: " + multipleMovedNodesCount.ToString());
        */
        //end debug
        
        int listLength;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (movedCrustNodes[j,i].Count == 0) // NO PLATE assigned to this space, start creating a rift
                {
                    CreateNewCrustMaterial(j, i);
                }
                else if (movedCrustNodes[j,i].Count == 1) // ONE PLATE assigned to this space
                {
                    BasicCrustMove(j, i);
                }
                else // MORE THAN ONE PLATE assigned to this space
                {
                    //clear plate searching dict
                    singlePlateSpacesCounts.Clear();

                    PlateInteraction(j, i, ref singlePlateSpacesCounts);
                }

                //Apply changes to corresponding vertex
                int vertIndex = j + (i * width);
                if (i % 2 == 0)
                {
                    verts[vertIndex].x = j * triWidth;
                    verts[vertIndex].y = crustNodes[j, i][0].Height;
                    verts[vertIndex].z = i * triHeight;
                }
                else
                {
                    verts[vertIndex].x = (j * triWidth) + halfTriWidth;
                    verts[vertIndex].y = crustNodes[j, i][0].Height;
                    verts[vertIndex].z = i * triHeight;
                }

                float h = verts[vertIndex].y;
                float normalisedHeight = (h - baseHeight) / maxHeight;
                //debug
                /*if (crustNodes[j,i][0].Plate.Type == PlateType.Oceanic)
                {
                    colors[vertIndex] = ColorExtended.ColorEx.oceanBlue;
                }
                else
                {
                    colors[vertIndex] = ColorExtended.ColorEx.sandBrown;
                }*/
                //end debug
                //colors[vertIndex] = stage.PickColour(normalisedHeight, seaLevel);
            }
        }

        for (int i = 0; i < height; i++)  
        {
            for (int j = 0; j < width; j++)
            {
                //clear any junk in movedNodes
                var currentNode = movedCrustNodes[j, i].First;
                listLength = movedCrustNodes[j, i].Count;
                for (int k = 0; k < listLength; k++)
                {
                    var nodeToDelete = currentNode;
                    ObjectPooler.current.ReturnNodeToPool(nodeToDelete.Value);
                    currentNode = currentNode.Next;
                    movedCrustNodes[j, i].Remove(nodeToDelete);
                }

                //temp
                /*
                if (crustNodes[j, i] == null)
                {
                    crustNodes[j, i] = new List<CrustNode>();
                    crustNodes[j, i].Add(new CrustNode(j, i));
                    crustNodes[j, i][0].Density = 0.1f;
                    crustNodes[j, i][0].Height = 100.0f;
                    numberOfNullNodes++;
                }
                if (crustNodes[j, i][0].Plate == null)
                {
                    crustNodes[j, i][0].Plate = p;
                    Debug.Log("null plate: " + j.ToString() + " " + i.ToString());
                    numberOfNodesWithNoPlate++;
                }
                */
                //endtemp
            }
        }
        /*Debug.Log("Null nodes = " + numberOfNullNodes.ToString());
        Debug.Log("Nodes with no plate = " + numberOfNodesWithNoPlate.ToString());*/


        //Now run a particle desposition step for each volcano in each of the lists of volcanos
        EruptVolcanos(shieldVolcanos, maxAge: 10, maxSearchRange: 4, maxElevationThreshold: 1, dropZoneRadius: 2);
        EruptVolcanos(stratoVolcanos, 5, 3, 1, 5, updateCoords: true);

        mesh.vertices = verts;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    private void MoveNodes()
    {
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                /*if (crustNodes[j, i].Count > 4)
                {
                    Debug.Log("More than 4 nodes in one crustNodes space: x=" + j.ToString() + " y=" + i.ToString());
                }*/

                //move the nodes
                for (int n_i = 0; n_i < crustNodes[j, i].Count; n_i++)
                {
                    //Get new x and y for prev node
                    CrustNode prevN = crustNodes[j, i][n_i];
                    int dx = prevN.Plate.XSpeed;
                    int dz = prevN.Plate.ZSpeed;
                    int newX = (prevN.X + dx) % width;
                    if (newX < 0) { newX = width + newX; }
                    int newZ = (prevN.Z + dz) % height;
                    if (newZ < 0) { newZ = height + newZ; }

                    //insert it at it's new position in movedNodes
                    var movedNode = ObjectPooler.current.GetPooledNode();
                    movedNode.Copy(prevN); //dereference
                    movedNode.X = newX;
                    movedNode.Z = newZ;
                    movedCrustNodes[newX, newZ].AddLast(movedNode);
                }
            }
        }

        //debug
        /*for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (movedCrustNodes[j, i].Count > 4)
                {
                    Debug.Log("More than 4 nodes in one movedCrustNodes space: x=" + j.ToString() + " y=" + i.ToString() + " | Count=" + movedCrustNodes[j,i].Count.ToString());
                }
            }
        }*/
        //end debug
    }



    private void CreateNewCrustMaterial(int xPos, int zPos)
    {
        crustNodes[xPos, zPos][0].Height = crustNodes[xPos, zPos][0].Height * 0.6f;

        //If the rift has become deep enough, random chance of new volcano
        if(crustNodes[xPos, zPos][0].Height < baseHeight * 0.2f)
        {
            float chance = Random.Range(0.0f, 1.0f);
            if (chance > 0.995f) // 1 in 1000 chance
            {
                if (chance > 0.9999f) // 1 in 1000 chance
                {
                    Volcano v = ObjectPooler.current.GetPooledVolcano();
                    v.X = xPos;
                    v.Z = zPos;
                    v.MaterialRate = 1000; //How many rocks get thrown out of the volcano each frame
                    this.AddStratoVolcano(v);
                }
                else
                {
                    Volcano v = ObjectPooler.current.GetPooledVolcano();
                    v.X = xPos;
                    v.Z = zPos;
                    v.MaterialRate = Random.Range(50, 80); //How many rocks get thrown out of the volcano each frame
                    this.AddShieldVolcano(v);
                }
            }
        }
    }

    private void BasicCrustMove(int xPos, int zPos)
    {
        //Clear space in main array
        int listLength = crustNodes[xPos, zPos].Count;
        for (int k = 0; k < listLength; k++)
        {
            ObjectPooler.current.ReturnNodeToPool(crustNodes[xPos, zPos][k]);
        }
        crustNodes[xPos, zPos].Clear();

        var movedCrustNode = ObjectPooler.current.GetPooledNode();
        movedCrustNode.Copy(movedCrustNodes[xPos, zPos].First.Value);

        crustNodes[xPos, zPos].Add(movedCrustNode);
    }

    private void PlateInteraction(int xPos, int zPos, ref Dictionary<Plate, int> singlePlateSpacesCounts)
    {
        //plates are overlapping, check which should subduct underneath (virtual), and which should be the surface (non-virtual)

        // if only one plate is non-virtual, do nothing
        bool oneNonVirtual = false;
        var currentNode = movedCrustNodes[xPos, zPos].First; // note: this refers to a LinkedListNode<T>, not a CrustNode
        for (int k = 0; k < movedCrustNodes[xPos, zPos].Count; k++)
        {
            if (!currentNode.Value.IsVirtual)
            {
                if (oneNonVirtual) { oneNonVirtual = false; break; } //multiple non-virtual nodes, break and go to plate interaction logic
                else { oneNonVirtual = true; }
            }
            currentNode = currentNode.Next;
        }

        if (!oneNonVirtual) //multiple non-virtual nodes
        {
            CollidePlates(xPos, zPos, ref singlePlateSpacesCounts);
        }

        //Clear space in main array
        int listLength = crustNodes[xPos, zPos].Count;
        for (int i = 0; i < listLength; i++)
        {
            ObjectPooler.current.ReturnNodeToPool(crustNodes[xPos, zPos][i]);
        }
        crustNodes[xPos, zPos].Clear();
        //crustNodes[xPos, zPos].TrimExcess();  <-- lower memory usage, higher processing cost

        //Place moved nodes into freed space
        currentNode = movedCrustNodes[xPos, zPos].First;
        listLength = movedCrustNodes[xPos, zPos].Count;
        for (int i = 0; i < listLength; i++)
        {
            //get rid of virtual nodes that are far below the surface
            if (!(currentNode.Value.IsVirtual && currentNode.Value.Height < baseHeight - 1.0f))
            {
                var movedCrustNode = ObjectPooler.current.GetPooledNode();
                movedCrustNode.Copy(currentNode.Value);

                crustNodes[xPos, zPos].Add(movedCrustNode);
            }
            currentNode = currentNode.Next;
        }
    }

    private void CollidePlates(int xPos, int zPos, ref Dictionary<Plate, int> singlePlateSpacesCounts)
    {
        bool hasOceanic = false, hasContinental = false, hasMultipleOc = false, hasMultipleCo = false;
        var currentNode = movedCrustNodes[xPos, zPos].First;
        for (int k = 0; k < movedCrustNodes[xPos, zPos].Count; k++)
        {
            if (currentNode.Value.Plate.Type == PlateType.Oceanic)
            {
                if (hasOceanic != true) { hasOceanic = true; }
                else { hasMultipleOc = true; }
            }
            else
            {
                if (hasContinental != true) { hasContinental = true; }
                else { hasMultipleCo = true; }
            }
            currentNode = currentNode.Next;
        }

        int listLength;
        //if O-O, Lowest density plate subducts
        if (!hasContinental)
        {
            int mostDense = 0;
            float highestDensity = 0.0f;
            currentNode = movedCrustNodes[xPos, zPos].First;
            for (int k = 0; k < movedCrustNodes[xPos, zPos].Count; k++)
            {
                if (currentNode.Value.Density > highestDensity)
                {
                    mostDense = k;
                    highestDensity = currentNode.Value.Density;
                }
                currentNode = currentNode.Next;
            }

            currentNode = movedCrustNodes[xPos, zPos].First;
            listLength = movedCrustNodes[xPos, zPos].Count;
            for (int k = 0; k < listLength; k++)
            {
                if (k != mostDense)
                {
                    currentNode.Value.IsVirtual = true;
                    currentNode.Value.Height = currentNode.Value.Height - (maxHeight * 0.05f); //subduct downwards
                    currentNode = currentNode.Next;
                }
                else
                {
                    currentNode.Value.IsVirtual = false;
                    if (k != 0) //no need to move it to the start if it's already there
                    {
                        movedCrustNodes[xPos, zPos].AddFirst(currentNode.Value);
                        var nodeToDelete = currentNode;
                        currentNode = currentNode.Next;
                        movedCrustNodes[xPos, zPos].Remove(nodeToDelete);
                    }
                }
            }
        }
        //if C-C, crunch
        else if (!hasOceanic)
        {
            //TEMPORARY, JUST USE THE OLD NAIVE CRUNCH
            currentNode = movedCrustNodes[xPos, zPos].First;
            for (int k = 0; k < movedCrustNodes[xPos, zPos].Count; k++)
            {
                if (!singlePlateSpacesCounts.ContainsKey(currentNode.Value.Plate))
                {
                    singlePlateSpacesCounts.Add(currentNode.Value.Plate, 0);
                }
                currentNode = currentNode.Next;
            }

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
                    CheckSpaceAndUpdateDict(xPos + x, zPos + z, ref singlePlateSpacesCounts, ref found); //top half
                    CheckSpaceAndUpdateDict(xPos + x, zPos - z, ref singlePlateSpacesCounts, ref found); //bottom half
                    if (diagnonalMove) { x++; }
                    z++;
                    diagnonalMove = !diagnonalMove;
                }
                // across top/bottom
                for (int c = 0; c < searchDistance; c++)
                {
                    CheckSpaceAndUpdateDict(xPos + x, zPos + z, ref singlePlateSpacesCounts, ref found);
                    CheckSpaceAndUpdateDict(xPos + x, zPos - z, ref singlePlateSpacesCounts, ref found);
                    x++;
                }
                // down/up to right corner
                for (int c = 0; c < searchDistance; c++)
                {
                    CheckSpaceAndUpdateDict(xPos + x, zPos + z, ref singlePlateSpacesCounts, ref found);
                    CheckSpaceAndUpdateDict(xPos + x, zPos - z, ref singlePlateSpacesCounts, ref found);
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

            CrustNode chosenNode = movedCrustNodes[xPos, zPos].First.Value;
            currentNode = movedCrustNodes[xPos, zPos].First;
            listLength = movedCrustNodes[xPos, zPos].Count;
            for (int k = 0; k < listLength; k++)
            {
                if (currentNode.Value.Plate == closestPlate)
                {
                    movedCrustNodes[xPos, zPos].AddFirst(currentNode.Value);
                    var nodeToDelete = currentNode;
                    currentNode = currentNode.Next;
                    movedCrustNodes[xPos, zPos].Remove(nodeToDelete);
                }
                else
                {
                    //Remove
                    ObjectPooler.current.ReturnNodeToPool(currentNode.Value);
                    var nodeToDelete = currentNode;
                    currentNode = currentNode.Next;
                    movedCrustNodes[xPos, zPos].Remove(nodeToDelete);
                }
            }

            movedCrustNodes[xPos, zPos].First.Value.IsVirtual = false;
            movedCrustNodes[xPos, zPos].First.Value.Height = crustNodes[xPos, zPos][0].Height * 1.02f; //Increase height as a result of collision (naive)
        }
        //if C-O, oceanic subducts
        else
        {
            if (hasMultipleCo)
            {
                //for now, do nothing
            }
            else if (hasMultipleOc)
            {
                //for now do nothing
            }
            else // just one O and one C
            {
                float subductedHeight;
                if (movedCrustNodes[xPos, zPos].First.Value.Plate.Type == PlateType.Oceanic) //O,C
                {
                    movedCrustNodes[xPos, zPos].First.Value.IsVirtual = true;
                    movedCrustNodes[xPos, zPos].First.Value.Height = movedCrustNodes[xPos, zPos].First.Value.Height - (maxHeight * 0.1f); //subduct downwards
                    subductedHeight = movedCrustNodes[xPos, zPos].First.Value.Height;

                    movedCrustNodes[xPos, zPos].Last.Value.IsVirtual = false;
                    //move to the start of the list
                    movedCrustNodes[xPos, zPos].AddFirst(movedCrustNodes[xPos, zPos].Last.Value);
                    movedCrustNodes[xPos, zPos].Remove(movedCrustNodes[xPos, zPos].Last);
                }
                else //C,O
                {
                    movedCrustNodes[xPos, zPos].First.Value.IsVirtual = false;
                    
                    movedCrustNodes[xPos, zPos].Last.Value.IsVirtual = true;
                    movedCrustNodes[xPos, zPos].Last.Value.Height = movedCrustNodes[xPos, zPos].Last.Value.Height - (maxHeight * 0.1f); //subduct downwards
                    subductedHeight = movedCrustNodes[xPos, zPos].Last.Value.Height;
                }

                //If the subducting plate is deep enough, random chance of new volcano
                if (movedCrustNodes[xPos, zPos].Last.Value.Height < baseHeight * 0.3f)
                {
                    float chance = Random.Range(0.0f, 1.0f);
                    if (chance > 0.999f)
                    {
                        Volcano v = ObjectPooler.current.GetPooledVolcano();
                        v.X = xPos;
                        v.Z = zPos;
                        v.MaterialRate = Random.Range(30, 60); //How many rocks get thrown out of the volcano each frame
                        this.AddStratoVolcano(v); //steep sided volcano
                    }
                }
            }
        }
    }



    //Implemented using particle deposition
    private void EruptVolcanos(List<Volcano> volcanos, int maxAge, int maxSearchRange, int maxElevationThreshold, int dropZoneRadius, bool updateCoords = false)
    {
        for (int v = 0; v < volcanos.Count; v++)
        {
			var vol = volcanos[v];
            //Increase age
            vol.Age++;
            //If it's at the end of it's lifetime, remove it from the list and return it to the volcano object pool
            if (vol.Age >= maxAge)
            {
                ObjectPooler.current.ReturnVolcanoToPool(vol);
                volcanos.RemoveAt(v);
            }
            else //otherwise, do eruption (particle deposition)
            {
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
						for(int range=1; range<=searchRange; range++)
						{
                            //do hexagon search and randomise side priority

                            bool diagnonalMove = true; //Start with a diagonal move when searching from an odd row
                            if (currentZ % 2 == 0)
                            {
                                diagnonalMove = false;
                            }

                            int side = Random.Range(0,3);
                            int searchX, searchZ;
                            switch (side) //Move search markers
                            {
                                case 0:
                                    searchX = -range; searchZ = 0;
                                    break;
                                case 1:
                                    if (diagnonalMove) { searchX = ((range - 1)/2 + 1) - range; }
                                    else { searchX = range/2 - range; }
                                    searchZ = range;
                                    break;
                                default: //case 2
                                    if (diagnonalMove) { searchX = range / 2 - range; }
                                    else { searchX = ((range - 1) / 2 + 1) - range;  }
                                    searchZ = range;
                                    break;
                            }

                            stable = true;
                            for (int s = 0; s<3; s++) //3 sets of parallel sides
							{
                                for (int c = 0; c < range; c++)
								{
                                    var currentNode = crustNodes[currentX, currentZ][0];
                                    int crustIndexX = (currentX + searchX) % width;
                                    if (crustIndexX < 0) { crustIndexX = width + crustIndexX; }
                                    if (Random.value > 0.5f)//Randomise which side to check first (we don't want to bias the drops)
                                    {
                                        int crustIndexZ = (currentZ + searchZ) % height;
                                        if (crustIndexZ < 0) { crustIndexZ = height + crustIndexZ; }
                                        var topHexagonNode = crustNodes[crustIndexX, crustIndexZ][0];

                                        if (currentNode.Height - topHexagonNode.Height > differenceFactor)
                                        {
                                            currentX = crustIndexX;
                                            currentZ = crustIndexZ;
                                            stable = false;
                                        }
                                        else
                                        {
                                            crustIndexZ = (currentZ - searchZ) % height;
                                            if (crustIndexZ < 0) { crustIndexZ = height + crustIndexZ; }
                                            var botHexagonNode = crustNodes[crustIndexX, crustIndexZ][0];

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
                                        var botHexagonNode = crustNodes[crustIndexX, crustIndexZ][0];

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
                                            var topHexagonNode = crustNodes[crustIndexX, crustIndexZ][0];

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
								if(side > 2)
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
                    crustNodes[currentX, currentZ][0].Height += rockSize;
                }
            }

            if (updateCoords)
            {
                vol.X = (vol.X + crustNodes[vol.X, vol.Z][0].Plate.XSpeed) % width;
                if (vol.X < 0) { vol.X = width + vol.X; }
            }
            else
            {
                vol.Z = (vol.Z + crustNodes[vol.X, vol.Z][0].Plate.ZSpeed) % height;
                if (vol.Z < 0) { vol.Z = height + vol.Z; }
            }
        }
    }




    private void CheckSpaceAndUpdateDict(int x, int z, ref Dictionary<Plate, int> plateCountsDict, ref bool found)
    {
        x = x % width;
        if (x < 0) { x = width + x; }
        z = z % height;
        if (z < 0) { z = height + z; }

        if (movedCrustNodes[x,z].Count == 1 && movedCrustNodes[x, z].First.Value.Plate != null)
        {
            if (plateCountsDict.ContainsKey(movedCrustNodes[x,z].First.Value.Plate))
            {
                plateCountsDict[movedCrustNodes[x,z].First.Value.Plate]++;
                found = true;
            }
        }
    }
}