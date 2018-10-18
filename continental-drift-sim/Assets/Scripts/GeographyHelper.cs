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

        // Constructor
        public Crust(int width, int height, float triWidth, float triHeight, MeshFilter mf, MeshRenderer mr, Mesh mesh = null, float defaultHeight = 0.0f, float seaLevel = 5.0f, Plate[] plates = null)
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

            mesh.RecalculateNormals();

            
            meshFilter.mesh = mesh;
            
            meshRenderer.material = Resources.Load("Materials/TestMaterial", typeof(Material)) as Material;

            Camera mainCam = Camera.main;
            mainCam.enabled = true;
            mainCam.aspect = 1;
            mainCam.transform.position = new Vector3(width * triWidth * 0.5f, 100.0f, height * triHeight * 0.5f);
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
            if (changes.GetLength(0) != heights.Length)
            {
                //Error!
                Debug.Log("changes and heights arrays are different sizes!");
            }

            for (int i=0; i<changes.GetLength(0); i++)
            {
                Debug.Log("i: " + i.ToString() + " , xPos: " + changes[i, 0] + " , yPos: " + changes[i, 1]);
                int xPos = changes[i,0];
                int yPos = changes[i,1];
                int vertIndex = yPos * width + xPos;

                float h = heights[i];

                verts[vertIndex] = new Vector3(verts[vertIndex].x, h, verts[vertIndex].z);
            }

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
        private int[,] outlinePlot; //not (get/set)able

        public Vector2[] Outline //ordered points representing plate outline
        {
            get { return this.outline; }
            set { this.outline = value; }
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
            set
            {
                this.crust = value;
                this.crust.AddPlate(this);
            }
        }


        public int[,] GetVertexPlot()
        {
            var lines = new int[outline.Length][,];
            //Debug.Log("created lines array of size: " + lines.GetLength(0).ToString());
            int plotCount = 0;
            for (int i=0; i<outline.Length; i++)
            {
                //Debug.Log("working on outline " + i.ToString());
                Vector2 p1 = outline[i];
                Vector2 p2;
                if(i == outline.Length - 1)
                {
                    p2 = outline[0];
                }
                else
                {
                    p2 = outline[i + 1];
                }

                // get the nearest points to the start and end of the line, and draw it to points
                int p1XCoord = (int)Math.Round(p1.x, 0);
                int p1YCoord = (int)Math.Round(p1.y, 0);
                int p2XCoord = (int)Math.Round(p2.x, 0);
                int p2YCoord = (int)Math.Round(p2.y, 0);
                //Debug.Log("Approximated coords - p1x: " + p1XCoord.ToString() + ", p1y: " + p1YCoord.ToString() + ", p2x: " + p2XCoord.ToString() + ", p2y: " + p2YCoord.ToString());

                //Debug.Log("About to call DrawLine()...");
                lines[i] = DrawLine(p1XCoord, p1YCoord, p2XCoord, p2YCoord);
                plotCount += lines[i].GetLength(0);
            }

            int[,] plots = new int[plotCount, 2];

            int plotIndex = 0;
            for (int i=0; i<lines.Length; i++) 
            {
                for (int j=0; j<lines[i].GetLength(0); j++)
                {
                    plots[plotIndex, 0] = lines[i][j, 0];
                    plots[plotIndex, 1] = lines[i][j, 1];
                    plotIndex++;
                }
            }

            this.outlinePlot = plots; //make plate remember its plot so we don't have to recalc if we want to use it later
            return plots;
        }

        private int[,] DrawLine(int x1, int y1, int x2, int y2)
        {
            var line = new List<int[]>();

            int w = x2 - x1;
            int h = y2 - y1;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
            int longest = Math.Abs(w);
            int shortest = Math.Abs(h);
            if (!(longest > shortest))
            {
                longest = Math.Abs(h);
                shortest = Math.Abs(w);
                if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
                dx2 = 0;
            }
            int numerator = longest >> 1;

            int xPrev = x1;
            int yPrev = y1;

            for (int i = 0; i <= longest; i++)
            {
                //Check if this creates a diagonal that cannot be represented
                if (y1 != yPrev && x1 != xPrev)
                {
                    if (y1 % 2 == 0)
                    {
                        if (xPrev == x1 + 1)
                        {
                            line.Add(new int[] { x1, yPrev });
                        }
                        else if (xPrev == x1 - 2)
                        {
                            line.Add(new int[] { x1 - 1, yPrev });
                        }
                    }
                    else
                    {
                        if (xPrev == x1 + 2)
                        {
                            line.Add(new int[] { x1 + 1, yPrev });
                        }
                        else if (xPrev == x1 - 1)
                        {
                            line.Add(new int[] { x1, yPrev});
                        }
                    }
                }

                line.Add(new int[] { x1, y1 });

                xPrev = x1;
                yPrev = y1;

                numerator += shortest;
                if (!(numerator < longest))
                {
                    numerator -= longest;
                    x1 += dx1;
                    y1 += dy1;
                }
                else
                {
                    x1 += dx2;
                    y1 += dy2;
                }
            }

            int[,] linePlot = new int[line.Count, 3];

            for (int i=0; i<line.Count; i++)
            {
                Debug.Log("i: " + i.ToString() + ", x: " + line[i][0].ToString() + ", y: " + line[i][1].ToString());
                linePlot[i, 0] = line[i][0];
                linePlot[i, 1] = line[i][1];
            }

            return linePlot;
        }

        public void DrawPlate()
        {
            int[,] prevPlot;
            if (outlinePlot != null)
            {
                prevPlot = outlinePlot;
            }
            else if (outline != null)
            {
                prevPlot = this.GetVertexPlot();
            }
            else
            {
                Debug.Log("No outline plot or outline for plate. Cancelling DrawPlate().");
                return;
            }

            var heights = new float[outlinePlot.GetLength(0)];

            //temp
            for (int i = 0; i < heights.Length; i++)
            {
                heights[i] = defaultHeight;
            }

            crust.UpdateMesh(outlinePlot, heights);
        }

        public void MovePlate()
        {
            int[,] prevPlot;
            if (outlinePlot != null)
            {
                prevPlot = outlinePlot;
            }
            else if (outline != null)
            {
                prevPlot = this.GetVertexPlot();
            }
            else
            {
                Debug.Log("No outline plot or outline for plate. Cancelling MovePlate().");
                return;
            }

            //Reset previous plate vertices
            var heights = new float[prevPlot.GetLength(0)];
            for (int i=0; i<heights.Length; i++)
            {
                heights[i] = crust.DefaultHeight;
            }

            crust.UpdateMesh(outlinePlot, heights);

            //Now draw plate in new posistion
            if(outline == null){
                Debug.Log("No outline for plate.");
            }

            for (int i=0; i<outline.Length; i++)
            {
                outline[i].x += xSpeed;
                outline[i].y += zSpeed;
            }

            outlinePlot = this.GetVertexPlot();

            //temp
            for (int i=0; i<heights.Length; i++)
            {
                heights[i] = this.DefaultHeight;
            }

            crust.UpdateMesh(outlinePlot, heights);
        }
    }

    public class OceanicPlate : Plate
    {

    }

    public class ContinentalPlate : Plate
    {

    }
}
