using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        private float defaultHeight;
        private float seaLevel;
        private Plate[] plates;


        // non-field definitions
        private int vertexCount;
        private int triCount;
        private Vector3[] verts;
        private int[] tris;
        private Color[] colors;

        // Constructor
        public Crust(MeshFilter mf, MeshRenderer mr, int width = 256, int height = 256, float triWidth = 1.0f, float triHeight = 1.0f, Mesh mesh = null, float defaultHeight = 0.0f, float seaLevel = 5.0f, Plate[] plates = null)
        {
            this.width = width;
            this.height = height;
            this.triWidth = triWidth;
            this.triHeight = triHeight;
            if (mesh == null) { this.mesh = new Mesh(); }
            else { this.mesh = mesh; }
            this.meshFilter = mf;
            this.meshRenderer = mr;
            this.defaultHeight = defaultHeight;
            this.seaLevel = seaLevel;
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
        public float DefaultHeight
        {
            get { return this.defaultHeight; }
            set { this.defaultHeight = value; }
        }
        public float SeaLevel
        {
            get { return this.seaLevel; }
            set { this.seaLevel = value; }
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

        public void BuildMesh()
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

            //vertices
            for (int i=0; i<verts.Length; i++)
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
            colors = new Color[verts.Length];

            for (int i=0; i<verts.Length; i++)
            {
                colors[i] = Color.Lerp(Color.white, Color.green, verts[i].y);
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
            mainCam.orthographicSize = height * triWidth * 0.5f;
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

        //takes a set of x,y coords and the heights to change them to
        public void UpdateMesh(int[,] changes, float[] heights)
        {
            Debug.Log(changes.GetLength(0).ToString());
            if (changes.GetLength(0) != heights.Length)
            {
                //Error!
                Debug.Log("changes and heights arrays are different sizes!");
            }

            for (int i=0; i<changes.GetLength(0); i++)
            {
                //Debug.Log("i: " + i.ToString() + " , xPos: " + changes[i, 0] + " , yPos: " + changes[i, 1]);
                int xPos = changes[i,0];
                int yPos = changes[i,1];
                int vertIndex = yPos * width + xPos;

                float h = heights[i];

                verts[vertIndex] = new Vector3(verts[vertIndex].x, h, verts[vertIndex].z);
            }

            /* inefficient, temporary colors alg */
            colors = new Color[verts.Length];

            for (int i = 0; i < verts.Length; i++)
            {
                colors[i] = Color.Lerp(Color.white, Color.green, verts[i].y);
            }
            mesh.colors = colors;

            mesh.vertices = verts;
            meshFilter.mesh = mesh;
        }
    }

    public class Plate
    {
        // field private vars
        private Vector2[] outline;
        private float defaultHeight = 5.0f;
        private float xSpeed = 0.0f;
        private float zSpeed = 0.0f;
        private Crust crust;

        // non-field definitions
        private int[,] plot; //not (get/set)able
        private float minX, maxX, minZ, maxZ;

        public Plate(Vector2[] outline = null, float defaultHeight = 5.0f, float xSpeed = 0.0f, float zSpeed = 0.0f, Crust crust = null)
        {
            this.outline = outline;
            this.defaultHeight = defaultHeight;
            this.xSpeed = xSpeed;
            this.zSpeed = zSpeed;
            if (crust != null) { crust.AddPlate(this); }

            this.SetBoundaries();
        }

        public Vector2[] Outline //ordered points representing plate outline
        {
            get { return this.outline; }
            set
            {
                this.outline = value;
                this.SetBoundaries();
            }
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
            if (outline != null)
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
            }
        }

        private int[,] GetVertexPlot()
        {
            int plotCount = 0;
            
            //Now fill it in
            int x1 = (int)Math.Round(minX, 0);
            int x2 = (int)Math.Round(maxX, 0);
            int z1 = (int)Math.Round(minZ, 0);
            int z2 = (int)Math.Round(maxZ, 0);

            Debug.Log(x1.ToString() + " " + x2.ToString() + " " + z1.ToString() + " " + z2.ToString() + " ");

            int[,] fillPlot = new int[(x2 - x1)*(z2 - z1) , 2];
            int fillPlotCount = 0;
            int polyCorners = outline.Length;
            int[] nodes = new int[z2-z1];
            int n;

            for (int z=z1; z <= z2; z++)
            {
                int nodeCount = 0;
                n = polyCorners - 1;

                int i = 0;
                for (i=0; i<polyCorners; i++)
                {
                    if (outline[i].y < z && outline[n].y >= z || outline[n].y < z && outline[i].y >= z)
                    {
                        nodes[nodeCount++] = (int)(outline[i].x + (z - outline[i].y) / (outline[n].y - outline[i].y) * (outline[n].x - outline[i].x));
                    }
                    n = i;
                }

                i = 0;
                while (i < nodeCount - 1)
                {
                    if (nodes[i]>nodes[i+1])
                    {
                        int swap = nodes[i];
                        nodes[i] = nodes[i + 1];
                        nodes[i + 1] = swap;

                        if (i != 0) { i--; }
                    }
                    else
                    {
                        i++;
                    }
                }

                for (i=0; i<nodeCount; i+=2)
                {
                    if (nodes[i] >= x2) break;
                    if (nodes[i + 1] > x1)
                    {
                        if (nodes[i] < x1) nodes[i] = x1;
                        if (nodes[i + 1] > x2) nodes[i + 1] = x2;
                        for (int x = nodes[i]; x < nodes[i + 1]; x++)
                        {
                            fillPlot[fillPlotCount,0] = x;
                            fillPlot[fillPlotCount,1] = z;
                            fillPlotCount++;
                            plotCount++;
                        }
                    }
                }
            }


            int[,] plots = new int[plotCount, 2];

            int plotIndex = 0;
            
            Debug.Log(fillPlotCount.ToString());
            for (int i=0; i<fillPlotCount; i++)
            {
                plots[plotIndex,0] = fillPlot[i,0];
                plots[plotIndex,1] = fillPlot[i,1];
                plotIndex++;
            }

            this.plot = plots; //make plate remember its plot so we don't have to recalc if we want to use it later
            return plots;
        }

        


        public void DrawPlate()
        {
            int[,] plot;
            //outline
            if (this.plot != null)
            {
                plot = this.plot;
            }
            else if (outline != null)
            {
                plot = this.GetVertexPlot();
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
            if (plot != null)
            {
                prevPlot = plot;
            }
            else if (outline != null)
            {
                prevPlot = this.GetVertexPlot();
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
                heights[i] = crust.DefaultHeight;
            }

            crust.UpdateMesh(plot, heights);

            //Now draw plate in new posistion
            if(outline == null){
                Debug.Log("No outline for plate.");
            }

            for (int i=0; i<outline.Length; i++)
            {
                outline[i].x += xSpeed;
                outline[i].y += zSpeed;
            }

            minX += xSpeed;
            maxX += xSpeed;
            minZ += zSpeed;
            maxZ += zSpeed;

            plot = this.GetVertexPlot();

            //temp
            heights = new float[plot.GetLength(0)];
            for (int i=0; i<heights.Length; i++)
            {
                heights[i] = this.DefaultHeight;
            }

            crust.UpdateMesh(plot, heights);
        }
    }

    public class OceanicPlate : Plate
    {

    }

    public class ContinentalPlate : Plate
    {

    }
}
