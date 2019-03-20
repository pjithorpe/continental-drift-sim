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
    private int width;
    private int height;
    private float triSize;
    private float maxHeight; //distance above base height, not total max height
    private float seaLevel; // between 0.0 and 1.0 representing positioning between baseHeight and maxHeight
    private float volcanoFrequency;
    private Plate[] plates;
    private List<CrustNode>[,] crustNodes; // array of nodes for the whole mesh, arranged in a matrix according to their positions
    private List<Volcano> stratoVolcanos;
    private List<Volcano> shieldVolcanos;


    // non-field definitions
    private float halfTriSize;
    private float spatialWidth; // (triWidth * width)
    private float spatialHeight; // (triHeight * height)
    private int vertexCount;
    private int triCount;
    private Vector3[] verts;
    private int[] tris;
    private Color[] colors;
    private LinkedList<CrustNode>[,] movedCrustNodes;
    private float subductionFactor;
    private float subductionVolcanoDepthThreshold; // how deep below the surface a subducting plate needs to be before it can produce surface volcanos
    private bool reEnergise = false;
    private bool randomiseMovement = false;
    private float seaLevelNext;
    private float volcanoFrequencyNext = 1f;

    //volcanos
    float shieldRockSize;
    float stratoRockSize;
    float shieldHeightSimilarityEpsilon; //in the particle deposition algoorithm, if heights are this distance away from eachother, they will be considered equal
    float stratoHeightSimilarityEpsilon;

    // Constructor
    public Crust(int width = 512, int height = 512, float triSize = 1f, float maxHeight = 10f, float seaLevel = 4.0f)
    {
        this.width = width;
        this.height = height;
        this.triSize = triSize;
        this.maxHeight = maxHeight;
        this.seaLevel = seaLevel;

        mesh = new Mesh();
        colors = new Color[width * height];
        plates = new Plate[0];
        shieldVolcanos = new List<Volcano>();
        stratoVolcanos = new List<Volcano>();
        crustNodes = MeshBuilder.BuildCrustNodesArray(width, height);
        movedCrustNodes = MeshBuilder.BuildMovedCrustNodesArray(width, height);

        halfTriSize = triSize / 2;
        spatialWidth = width * triSize;
        spatialHeight = height * triSize;
        subductionFactor = maxHeight * 0.05f;
        subductionVolcanoDepthThreshold = maxHeight * 0.15f;
        shieldRockSize = maxHeight / 60f;
        shieldHeightSimilarityEpsilon = shieldRockSize * 0.2f;
        stratoRockSize = maxHeight / 20f;
        stratoHeightSimilarityEpsilon = stratoRockSize * 0.2f;

        seaLevelNext = Mathf.InverseLerp(0.0f, maxHeight, seaLevel);
        volcanoFrequency = 1f;
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
    public float MaxHeight
    {
        get { return this.maxHeight; }
        set { this.maxHeight = value; }
    }
    public float SeaLevel
    {
        get { return this.seaLevel; }
        set { this.seaLevelNext = value; }
    }
    public float VolcanoFrequency
    {
        get { return this.volcanoFrequency; }
        set { this.volcanoFrequencyNext = value; }
    }
    public List<CrustNode>[,] CrustNodes
    {
        get { return this.crustNodes; }
        set { this.crustNodes = value; }
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

    public void RandomisePlateMovements()
    {
        randomiseMovement = true;
    }
    public void ReEnergisePlates()
    {
        reEnergise = true;
    }



    MaterialType[] types = new MaterialType[5];
    Dictionary<Plate, int> singlePlateSpacesCounts = new Dictionary<Plate, int>();

    public Mesh UpdateMesh()
    {
        //Check for changed controls
        //re-energise system
        if (reEnergise)
        {
            for (int p = 0; p < plates.Length; p++)
            {
                plates[p].AccurateXSpeed += 1f;
                plates[p].AccurateZSpeed += 1f;
            }
            reEnergise = false;
        }
        //randomise plate direction
        if (randomiseMovement)
        {
            for (int p = 0; p < plates.Length; p++)
            {
                plates[p].AccurateXSpeed = 0;
                plates[p].AccurateZSpeed = 0;
                while ((plates[p].XSpeed < 0.3f && plates[p].XSpeed > -0.3f) && (plates[p].ZSpeed < 0.3f && plates[p].ZSpeed > -0.3f))
                {
                    plates[p].AccurateXSpeed = Random.Range(-2f, 2f);
                    plates[p].AccurateZSpeed = Random.Range(-2f, 2f);
                }
            }
            randomiseMovement = false;
        }
        seaLevel = maxHeight * seaLevelNext;
        volcanoFrequency = volcanoFrequencyNext;

        MoveNodes(); // for every node, move it based on its plate's speed

        //Sort out empty regions first (cause by constructive margins)
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (movedCrustNodes[j, i].Count == 0) // NO PLATE assigned to this space, start creating a rift
                {
                    CreateNewCrustMaterial(j, i);
                }
            }
        }

        int listLength;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (movedCrustNodes[j,i].Count != 0)
                {
                    if (movedCrustNodes[j, i].Count == 1) // ONE PLATE assigned to this space
                    {
                        BasicCrustMove(j, i);
                    }
                    else // MORE THAN ONE PLATE assigned to this space
                    {
                        //clear plate searching dict
                        singlePlateSpacesCounts.Clear();

                        Tectonics.PlateInteraction(j, i, this, ref crustNodes, ref movedCrustNodes, ref singlePlateSpacesCounts, subductionFactor, subductionVolcanoDepthThreshold, volcanoFrequency);
                    }

                    //Apply changes to corresponding vertex
                    int vertIndex = j + (i * width);
                    if (i % 2 == 0)
                    {
                        verts[vertIndex].x = j * triSize;
                        verts[vertIndex].y = crustNodes[j, i][0].Height;
                        verts[vertIndex].z = i * triSize;
                    }
                    else
                    {
                        verts[vertIndex].x = (j * triSize) + halfTriSize;
                        verts[vertIndex].y = crustNodes[j, i][0].Height;
                        verts[vertIndex].z = i * triSize;
                    }

                    //colour mesh
                    colors[vertIndex] = ColorExtended.ColorTerrain.CalculateColor(crustNodes[j, i][0], seaLevel, maxHeight);
                }
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
            }
        }

        //Now run a particle desposition step for each volcano in each of the lists of volcanos
        EruptVolcanos(shieldVolcanos, maxAge: 3, maxSearchRange: 4, maxElevationThreshold: 1, dropZoneRadius: 4, rockSize: shieldRockSize, heightSimilarityEpsilon: shieldHeightSimilarityEpsilon);
        EruptVolcanos(stratoVolcanos, 2, 2, 2, 3, stratoRockSize, stratoHeightSimilarityEpsilon);

        mesh.vertices = verts;
        mesh.colors = colors;
        mesh.RecalculateNormals();

        return mesh;
    }

    private void MoveNodes()
    {
		for(int p = 0; p < plates.Length; p++)
		{
			plates[p].RegisterMovement();
		}

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                //move the nodes
                for (int n_i = 0; n_i < crustNodes[j, i].Count; n_i++)
                {
                    //Get new x and y for prev node
                    CrustNode prevN = crustNodes[j, i][n_i];
							
					int newX, newZ;
					if(prevN.Plate.CheckMoveX())
					{
						int dx = prevN.Plate.XSpeed;
						newX = (prevN.X + dx) % width;
						if (newX < 0) { newX = width + newX; }
					}
					else
					{
						newX = prevN.X;
					}
					if(prevN.Plate.CheckMoveZ())
					{
						int dz = prevN.Plate.ZSpeed;
						newZ = (prevN.Z + dz) % height;
						if (newZ < 0) { newZ = height + newZ; }
					}
					else
					{
						newZ = prevN.Z;
					}

                    //insert it at it's new position in movedNodes
                    var movedNode = ObjectPooler.current.GetPooledNode();
                    movedNode.Copy(prevN); //dereference
                    movedNode.X = newX;
                    movedNode.Z = newZ;
                    movedCrustNodes[newX, newZ].AddLast(movedNode);
                }
            }
        }

        // update plate speeds
        for (int p = 0; p < plates.Length; p++)
        {
            plates[p].ApplyVectorAffectors();
            plates[p].RecalculateMass();
        }
    }


    private void CreateNewCrustMaterial(int xPos, int zPos)
    {
        crustNodes[xPos, zPos][0].Height = crustNodes[xPos, zPos][0].Height * 0.8f;
        crustNodes[xPos, zPos][0].Plate.AffectPlateVector();

        //If the rift has become deep enough, random chance of new volcano, and start producing oceanic crust
        if(crustNodes[xPos, zPos][0].Height < seaLevel * 0.5f)
        {
            float chance = Random.Range(0.0f, 1.0f);
            if (chance < (0.001f * volcanoFrequency)) // 1 in 20 chance
            {
                Volcano v = ObjectPooler.current.GetPooledVolcano();
                v.X = xPos;
                v.Z = zPos;
                v.MaterialRate = Random.Range(50, 500); //How many rocks get thrown out of the volcano each frame
                this.AddShieldVolcano(v);
            }

            crustNodes[xPos, zPos][0].Type = MaterialType.Oceanic;
        }

        //Finally, add the node to the movedNodes array so that there aren't empty spots which will cause errors for other interactions
        var newNode = ObjectPooler.current.GetPooledNode();
        newNode.Copy(crustNodes[xPos, zPos][0]); //dereference
        newNode.X = xPos;
        newNode.Z = zPos;
        movedCrustNodes[xPos, zPos].AddLast(newNode);
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



    //Implemented using particle deposition
    private void EruptVolcanos(List<Volcano> volcanos, int maxAge, int maxSearchRange, int maxElevationThreshold, int dropZoneRadius, float rockSize, float heightSimilarityEpsilon, bool updateCoords = false)
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
                crustNodes = TerrainGeneration.ParticleDeposition(crustNodes, vol, rockSize, heightSimilarityEpsilon, dropZoneRadius, maxSearchRange, maxElevationThreshold);
            }

            if (updateCoords)
            {
                if (crustNodes[vol.X, vol.Z][0].Plate.CheckMoveX())
                {
                    vol.X = (vol.X + crustNodes[vol.X, vol.Z][0].Plate.XSpeed) % width;
                    if (vol.X < 0) { vol.X = width + vol.X; }
                }

                if (crustNodes[vol.X, vol.Z][0].Plate.CheckMoveZ())
                {
                    vol.Z = (vol.Z + crustNodes[vol.X, vol.Z][0].Plate.ZSpeed) % height;
                    if (vol.Z < 0) { vol.Z = height + vol.Z; }
                }
            }
        }
    }
}