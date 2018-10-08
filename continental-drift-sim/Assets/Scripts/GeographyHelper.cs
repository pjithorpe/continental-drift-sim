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

        public void SetSpeed(float s)
        {
            speed = s;
        }

        public List<Vector2[]> GetVertexPlot()
        {
            var lines = new List<Vector2[]>();
            for (int i=0; i<outline.Length; i++)
            {
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
                lines.Add(DrawLine((int)Math.Round(p1.x, 0), (int)Math.Round(p1.y, 0), (int)Math.Round(p2.x, 0), (int)Math.Round(p2.y, 0)));
            }

            return lines;
        }

        private Vector2[] DrawLine(int x1, int y1, int x2, int y2)
        {
            var line = new List<Vector2>();

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
                line.Add(new Vector2(x1, y1));
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

            return line.ToArray();
        }
    }

    public class OceanicPlate : Plate
    {

    }

    public class ContinentalPlate : Plate
    {

    }
}
