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
    private List<Volcano> volcanos;


    // non-field definitions
    private float halfTriWidth;
    private int vertexCount;
    private int triCount;
    private Vector3[] verts;
    private int[] tris;
    private Color[] colors;
    private LinkedList<CrustNode>[,] movedCrustNodes;

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
        crustNodes = new List<CrustNode>[width, height];
        movedCrustNodes = new LinkedList<CrustNode>[width, height];
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
                        verts[vertIndex] = new Vector3(newX * triWidth, newNodes[newX, newZ][0].Height, newZ * triHeight);
                    }
                    else
                    {
                        verts[vertIndex] = new Vector3((newX * triWidth) + halfTriWidth, newNodes[newX, newZ][0].Height, newZ * triHeight);
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





    public void UpdateMesh()
    {
        var t = crustNodes[231, 0][0];

        MoveNodes(); // for every node, move it 

        var bbb = movedCrustNodes[500, 250];

        //debug
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
        //end debug

        PlateType[] types = new PlateType[5000];
        Dictionary<Plate, int> singlePlateSpacesCounts = new Dictionary<Plate, int>();
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if(j==500 && i==250)
                {
                    var hehehe = 0;
                }
                if (movedCrustNodes[j,i].Count == 0) // NO PLATE assigned to this space, start creating a rift
                {
                    CreateNewCrustMaterial(j, i);
                }
                else if (movedCrustNodes[j,i].Count == 1) // ONE PLATE assigned to this space
                {
                    crustNodes[j,i][0].Copy(movedCrustNodes[j,i].First.Value);
                }
                else // MORE THAN ONE PLATE assigned to this space
                {
                    PlateInteraction(j, i, ref types, ref singlePlateSpacesCounts);
                }

                //Apply changes to corresponding vertex
                int vertIndex = j + (i * width);
                if (i % 2 == 0)
                {
                    verts[vertIndex] = new Vector3(j * triWidth, crustNodes[j,i][0].Height, i * triHeight);
                }
                else
                {
                    verts[vertIndex] = new Vector3((j * triWidth) + halfTriWidth, crustNodes[j,i][0].Height, i * triHeight);
                }

                float h = verts[vertIndex].y;
                float normalisedHeight = (h - baseHeight) / maxHeight;
                colors[vertIndex] = stage.PickColour(normalisedHeight, seaLevel);
            }
        }

        int numberOfNullNodes = 0;
        int numberOfNodesWithNoPlate = 0;
        int listLength;
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
                //endtemp
            }
        }
        Debug.Log("Null nodes = " + numberOfNullNodes.ToString());
        Debug.Log("Nodes with no plate = " + numberOfNodesWithNoPlate.ToString());


        //Now run a particle desposition step for each volcano
        EruptVolcanos();

        mesh.vertices = verts;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        var x = crustNodes[231, 0][0];
    }

    private void MoveNodes()
    {
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (crustNodes[j, i].Count > 4)
                {
                    Debug.Log("More than 4 nodes in one crustNodes space: x=" + j.ToString() + " y=" + i.ToString());
                }

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

                    //debug
                    if(newX == 500 && newZ== 250)
                    {
                        prevN = crustNodes[j, i][n_i];
                        dx = prevN.Plate.XSpeed;
                        dz = prevN.Plate.ZSpeed;
                        newX = (prevN.X + dx) % width;
                        if (newX < 0) { newX = width + newX; }
                        newZ = (prevN.Z + dz) % height;
                        if (newZ < 0) { newZ = height + newZ; }
                    }
                    else if (newX == 999 && newZ == 499)
                    {
                        prevN = crustNodes[j, i][n_i];
                        dx = prevN.Plate.XSpeed;
                        dz = prevN.Plate.ZSpeed;
                        newX = (prevN.X + dx) % width;
                        if (newX < 0) { newX = width + newX; }
                        newZ = (prevN.Z + dz) % height;
                        if (newZ < 0) { newZ = height + newZ; }
                    }
                    //end debug

                    //insert it at it's new position in movedNodes
                    var movedNode = ObjectPooler.current.GetPooledNode();//dereference
                    movedNode.Copy(prevN);
                    movedNode.X = newX;
                    movedNode.Z = newZ;
                    movedCrustNodes[newX, newZ].AddLast(movedNode);
                }
            }
        }

        //debug
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (movedCrustNodes[j, i].Count > 4)
                {
                    Debug.Log("More than 4 nodes in one movedCrustNodes space: x=" + j.ToString() + " y=" + i.ToString() + " | Count=" + movedCrustNodes[j,i].Count.ToString());
                }
            }
        }
        //end debug
    }


    private void CreateNewCrustMaterial(int xPos, int zPos)
    {
        crustNodes[xPos, zPos][0].Height += crustNodes[xPos, zPos][0].Height * 0.5f;

        //Random chance of volcanic eruption
        float chance = Random.Range(0.0f, 1.0f);
        if (chance > 0.995f) // 1 in 200 chance
        {
            Volcano v = ObjectPooler.current.GetPooledVolcano();
            v.X = xPos;
            v.Z = zPos;
            v.MaterialRate = Random.Range(0, 6); //How many rocks get thrown out of the volcano each frame
            this.AddVolcano(v);
        }
    }

    private void PlateInteraction(int xPos, int zPos, ref PlateType[] types, ref Dictionary<Plate, int> singlePlateSpacesCounts)
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
            CollidePlates(xPos, zPos, ref types, ref singlePlateSpacesCounts);
        }

        crustNodes[xPos, zPos][0] = movedCrustNodes[xPos, zPos].First.Value;
        currentNode = movedCrustNodes[xPos, zPos].First.Next;
        int listLength = movedCrustNodes[xPos, zPos].Count;
        for (int k = 1; k < listLength; k++)
        {
            //get rid of virtual nodes that are far below the surface
            if (currentNode.Value.IsVirtual && currentNode.Value.Height < baseHeight - 1.0f)
            {
                ObjectPooler.current.ReturnNodeToPool(currentNode.Value);
                var nodeToDelete = currentNode;
                currentNode = currentNode.Next;
                movedCrustNodes[xPos, zPos].Remove(nodeToDelete);
            }
            else
            {
                //Add node to main nodes array
                if (k < crustNodes[xPos, zPos].Count)
                {
                    crustNodes[xPos, zPos][k] = currentNode.Value;
                }
                else
                {
                    crustNodes[xPos, zPos].Add(currentNode.Value);
                }
                currentNode = currentNode.Next;
            }
        }
        //remove any excess
        if (crustNodes[xPos, zPos].Count > listLength)
        {
            crustNodes[xPos, zPos].RemoveRange(listLength, crustNodes[xPos, zPos].Count - listLength);
        }
    }

    private void CollidePlates(int xPos, int zPos, ref PlateType[] types, ref Dictionary<Plate, int> singlePlateSpacesCounts)
    {
        bool hasOceanic = false, hasContinental = false, hasMultipleOc = false, hasMultipleCo = false;
        var currentNode = movedCrustNodes[xPos, zPos].First;
        for (int k = 0; k < movedCrustNodes[xPos, zPos].Count; k++)
        {
            types[k] = currentNode.Value.Plate.Type;
            if (types[k] == PlateType.Oceanic)
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
            movedCrustNodes[xPos, zPos].First.Value.Height = crustNodes[xPos, zPos][0].Height * 1.01f; //Increase height as a result of collision (naive)
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
                if (movedCrustNodes[xPos, zPos].First.Value.Plate.Type == PlateType.Oceanic) //O,C
                {
                    movedCrustNodes[xPos, zPos].First.Value.IsVirtual = true;
                    movedCrustNodes[xPos, zPos].First.Value.Height = movedCrustNodes[xPos, zPos].First.Value.Height - (maxHeight * 0.05f); //subduct downwards

                    movedCrustNodes[xPos, zPos].Last.Value.IsVirtual = false;
                    //move to the start of the list
                    movedCrustNodes[xPos, zPos].AddFirst(movedCrustNodes[xPos, zPos].Last.Value);
                    movedCrustNodes[xPos, zPos].Remove(movedCrustNodes[xPos, zPos].Last);
                }
                else //C,O
                {
                    movedCrustNodes[xPos, zPos].First.Value.IsVirtual = false;

                    movedCrustNodes[xPos, zPos].Last.Value.IsVirtual = true;
                    movedCrustNodes[xPos, zPos].Last.Value.Height = movedCrustNodes[xPos, zPos].Last.Value.Height - (maxHeight * 0.05f); //subduct downwards
                }
            }
        }
    }


    private void EruptVolcanos()
    {
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
    }

    private void CheckSpaceAndUpdateDict(int x, int z, ref Dictionary<Plate, int> plateCountsDict, ref bool found)
    {
        x = x % (width - 1);
        if (x < 0) { x = width + x; }
        z = z % (height - 1);
        if (z < 0) { z = height + z; }

        if (movedCrustNodes[x,z].Count == 1 && movedCrustNodes[x, z].First.Value.Plate != null)
        {
            if (plateCountsDict.ContainsKey(movedCrustNodes[x,z].First.Value.Plate))
            {
                plateCountsDict[ movedCrustNodes[x,z].First.Value.Plate ]++;
                found = true;
            }
        }
    }

}