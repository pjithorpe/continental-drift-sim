using UnityEngine;
using UnityEditor;

public static class Angles
{
    public static float Similarity(float x1, float y1, float x2, float y2)
    {
        return Mathf.Acos(((x1*x2)+(y1*y2))/Mathf.Sqrt(x1*x1 + y1*y1)*Mathf.Sqrt(x2*x2 + y2*y2));
    }
}