using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GeographyHelper
{
    public class Plate
    {
        protected Vector2[] outline; //ordered points representing plate outline
        protected float defaultHeight; //default height of vertices inside the plate
        protected float speed;

        public void SetOutline(Vector2[] oL)
        {
            outline = oL;
        }

        public void SetDefaultHeight(float dH)
        {
            defaultHeight = dH;
        }

        public float GetDefaultHeight()
        {
        	return defaultHeight;
        }

        public void SetSpeed(float s)
        {
            speed = s;
        }

        public int[,] GetVertexPlot()
        {
            var lines = new int[outline.Length][,];
            Debug.Log("created lines array of size: " + lines.GetLength(0).ToString());
            int plotCount = 0;
            for (int i=0; i<outline.Length; i++)
            {
                Debug.Log("working on outline " + i.ToString());
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
                Debug.Log("Approximated coords - p1x: " + p1XCoord.ToString() + ", p1y: " + p1YCoord.ToString() + ", p2x: " + p2XCoord.ToString() + ", p2y: " + p2YCoord.ToString());

                Debug.Log("About to call DrawLine()...");
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
            for (int i = 0; i <= longest; i++)
            {
                //Check if this creates a diagonal that cannot be represented
                if ((y1 % 2 == 1) && (line[-1][1] != y1))
                {
                    if(Math.Abs(line[-1][0] - x1) == 1)
                    {
                        line.Add(new int[] { });
                    }
                    else if(Math.Abs(line[-1][0] - x1) == 2)
                    {
                        line.Add(new int[] { });
                    }
                }

                line.Add(new int[] { x1, y1 });

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
    }

    public class OceanicPlate : Plate
    {

    }

    public class ContinentalPlate : Plate
    {

    }
}
