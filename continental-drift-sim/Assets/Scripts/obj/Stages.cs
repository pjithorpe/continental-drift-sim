using System;
using UnityEngine;

using ColorExtended;


    /*********
     * STAGES
     *********/

    public interface Stage
    {
        Color PickColour(float normailsedHeight, float seaLevel);
    }

    public class CoolingStage : Stage
    {
        public Color PickColour(float normalisedHeight, float seaLevel)
        {
            if (normalisedHeight <= seaLevel)
            {
                return Color.Lerp(Color.yellow, Color.red, normalisedHeight / seaLevel);
            }
            else if (seaLevel < normalisedHeight && normalisedHeight < seaLevel + 0.2f)
            {
                return ColorEx.bedrockGrey;
            }
            else
            {
                return Color.Lerp(ColorEx.bedrockGrey, ColorEx.mountainGrey, (normalisedHeight - (seaLevel + 0.2f)) / (1 - (seaLevel + 0.2f)));
            }
        }
    }

    public class WaterStage: Stage
    {
        public Color PickColour(float normalisedHeight, float seaLevel)
        {
            return Color.Lerp(ColorEx.bedrockGrey, ColorEx.mountainGrey, normalisedHeight);
        }
    } 

    public class LifeStage : Stage
    {
        public Color PickColour(float normalisedHeight, float seaLevel)
        {
            if (normalisedHeight <= 0.45f)
            {
                return Color.Lerp(ColorEx.bedrockGrey, ColorEx.mountainGrey, normalisedHeight / 0.45f);
            }
            if ((0.45f < normalisedHeight) && (normalisedHeight < 0.55f))
            {
                return ColorEx.sandBrown;
            }
            else
            {
                return Color.Lerp(ColorEx.bedrockGrey, Color.green, normalisedHeight);
            }
        }
    }
