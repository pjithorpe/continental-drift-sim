using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeographyHelper
{
	public class Crust
	{
		public Mesh Mesh { get; set; }
		public float DefaultHeight { get; set; }
		public float SeaLevel { get; set; }

		private Plate[] plates;

		public Plate[] GetPlates()
		{
			return plates;
		}

		public void SetPlates(Plate[] ps)
		{
			for (int i=0; i<plates.Length; i++)
			{
				plates[i].SetCrust(this);
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
	}

    public class Plate
    {
        public Vector2[] Outline { get; set; } //ordered points representing plate outline
        public float DefaultHeight { get; set; } //default height of vertices inside the plate
        public float XSpeed { get; set; }
        public float ZSpeed { get; set; }

        private Crust crust;

        public Crust GetCrust()
        {
        	return crust;
        }

        public void SetCrust(Crust cr)
        {
        	crust = cr;
        	crust.AddPlate(this);
        }



        public int[,] GetVertexPlot()
        {
            var lines = new int[Outline.Length][,];
            //Debug.Log("created lines array of size: " + lines.GetLength(0).ToString());
            int plotCount = 0;
            for (int i=0; i<Outline.Length; i++)
            {
                //Debug.Log("working on outline " + i.ToString());
                Vector2 p1 = Outline[i];
                Vector2 p2;
                if(i == Outline.Length - 1)
                {
                    p2 = Outline[0];
                }
                else
                {
                    p2 = Outline[i + 1];
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

        public void MovePlate() {

        }
    }

    public class OceanicPlate : Plate
    {

    }

    public class ContinentalPlate : Plate
    {

    }
}
