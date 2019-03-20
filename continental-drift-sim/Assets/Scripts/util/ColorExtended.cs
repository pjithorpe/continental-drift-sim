using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ColorExtended {

    public static class ColorEx
    {
        public static readonly Color oceanLightBlue = new Color32(28, 107, 160, 255);
        public static readonly Color oceanDeepBlue = new Color32(11, 54, 97, 255);
        public static readonly Color oceanShallowsBlue = new Color32(0, 162, 210, 255);
        public static readonly Color bedrockGrey = new Color32(79, 70, 60, 255);
        public static readonly Color mountainGrey = new Color32(42, 42, 42, 255);
        public static readonly Color sandBrownLight = new Color32(255, 236, 173, 255);
        public static readonly Color sandBrownDark = new Color32(195, 131, 76, 255);
        public static readonly Color forestGreenLight = new Color32(70, 163, 70, 255);
        public static readonly Color forestGreenDark = new Color32(10, 56, 10, 255);
    }

    public static class ColorTerrain
    {
        public static Color CalculateColor(CrustNode n, float seaLevel, float maxHeight)
        {
            float h = n.Height;
            float normalisedHeight;

            if (n.Type == MaterialType.Oceanic)
            {
                normalisedHeight = h / seaLevel;
                if (normalisedHeight > 1f)
                {
                    return ColorEx.sandBrownLight;
                }
                else
                {
                    return Color.Lerp(ColorEx.oceanDeepBlue, ColorEx.oceanLightBlue, normalisedHeight);
                }

            }
            else
            {
                if (h < seaLevel)
                {
                    normalisedHeight = h / seaLevel;
                    return Color.Lerp(ColorEx.oceanDeepBlue, ColorEx.oceanShallowsBlue, normalisedHeight);
                }
                else
                {
                    h -= seaLevel;
                    if (h < maxHeight * 0.05f) //coast
                    {
                        normalisedHeight = h / maxHeight * 0.05f;
                        return Color.Lerp(ColorEx.sandBrownLight, ColorEx.sandBrownDark, normalisedHeight);
                    }
                    else if (h < maxHeight * 0.5f) //land
                    {
                        normalisedHeight = Mathf.InverseLerp(0.0f, maxHeight * 0.45f, h - maxHeight * 0.05f);
                        return Color.Lerp(ColorEx.forestGreenLight, ColorEx.forestGreenDark, normalisedHeight);
                    }
                    else //mountains
                    {
                        normalisedHeight = Mathf.InverseLerp(0.0f, maxHeight * 1f, h - maxHeight * 0.5f);
                        return Color.Lerp(ColorEx.mountainGrey, Color.white, normalisedHeight);
                    }
                }
            }
        }
    }
}