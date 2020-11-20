using System.Collections.Generic;
using UnityEngine;

public class StarGlow : MonoBehaviour
{
    #region Enum

    public enum CompositeType
    {
        _COMPOSITE_TYPE_ADDITIVE         = 0,
        _COMPOSITE_TYPE_SCREEN           = 1,
        _COMPOSITE_TYPE_COLORED_ADDITIVE = 2,
        _COMPOSITE_TYPE_COLORED_SCREEN   = 3,
        _COMPOSITE_TYPE_DEBUG            = 4
    }

    #endregion Enum

    #region Field

    private static Dictionary<CompositeType, string> CompositeTypes = new Dictionary<CompositeType, string>()
    {
        { CompositeType._COMPOSITE_TYPE_ADDITIVE,         CompositeType._COMPOSITE_TYPE_ADDITIVE.ToString()         },
        { CompositeType._COMPOSITE_TYPE_SCREEN,           CompositeType._COMPOSITE_TYPE_SCREEN.ToString()           },
        { CompositeType._COMPOSITE_TYPE_COLORED_ADDITIVE, CompositeType._COMPOSITE_TYPE_COLORED_ADDITIVE.ToString() },
        { CompositeType._COMPOSITE_TYPE_COLORED_SCREEN,   CompositeType._COMPOSITE_TYPE_COLORED_SCREEN.ToString()   },
        { CompositeType._COMPOSITE_TYPE_DEBUG,            CompositeType._COMPOSITE_TYPE_DEBUG.ToString()            }
    };

    public StarGlow.CompositeType compositeType = StarGlow.CompositeType._COMPOSITE_TYPE_ADDITIVE;

    [Range(0, 1)]
    public float threshold = 1;

    [Range(0, 10)]
    public float intensity = 1;

    [Range(1, 20)]
    public int divide = 3;

    [Range(1, 5)]
    public int iteration = 5;

    [Range(0, 1)]
    public float attenuation = 1;

    [Range(0, 360)]
    public float angleOfStreak = 0;

    [Range(1, 16)]
    public int numOfStreaks = 4;

    public Material material;

    public Color color = Color.white;

    private int compositeTexID   = 0;
    private int compositeColorID = 0;
    private int brightnessSettingsID   = 0;
    private int iterationID      = 0;
    private int offsetID         = 0;

    #endregion Field

    #region Method

    void Start()
    {
        compositeTexID   = Shader.PropertyToID("_CompositeTex");
        compositeColorID = Shader.PropertyToID("_CompositeColor");
        brightnessSettingsID   = Shader.PropertyToID("_BrightnessSettings");
        iterationID      = Shader.PropertyToID("_Iteration");
        offsetID         = Shader.PropertyToID("_Offset");
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RenderTexture brightnessTex = RenderTexture.GetTemporary(source.width  / divide,
                                                                 source.height / divide,
                                                                 source.depth,
                                                                 source.format);
        RenderTexture blurredTex1   = RenderTexture.GetTemporary(brightnessTex.descriptor);
        RenderTexture blurredTex2   = RenderTexture.GetTemporary(brightnessTex.descriptor);
        RenderTexture compositeTex  = RenderTexture.GetTemporary(brightnessTex.descriptor);

        // STEP:1
        // Get resized brightness image.

        material.SetVector
        (brightnessSettingsID, new Vector3(threshold, intensity, attenuation));

        Graphics.Blit(source, brightnessTex, material, 1);

        // DEBUG:
        //Graphics.Blit(brightnessTex, destination, material, 0);
        //return;

        // STEP:2
        // Get blurred brightness image.

        float angle = 360f / numOfStreaks;

        for (int x = 1; x <= numOfStreaks; x++)
        {
            Vector2 offset =
            (Quaternion.AngleAxis(angle * x + angleOfStreak, Vector3.forward) * Vector2.down).normalized;

            material.SetVector(offsetID, offset);

            material.SetInt(iterationID, 1);

            Graphics.Blit(brightnessTex, blurredTex1, material, 2);

            // DEBUG:
            //Graphics.Blit(blurredTex1, destination, material, 0);
            //return;

            for (int i = 2; i <= iteration; i++)
            {
                material.SetInt(iterationID, i);

                Graphics.Blit(blurredTex1, blurredTex2, material, 2);

                // DEBUG:
                // Graphics.Blit(blurredTex2, destination, base.material, 0);
                // return;

                RenderTexture temp = blurredTex1;
                blurredTex1 = blurredTex2;
                blurredTex2 = temp;
            }

            Graphics.Blit(blurredTex1, compositeTex, material, 3);
        }

        // DEBUG:
        //Graphics.Blit(compositeTex, destination, material, 0);
        //return;

        // STEP:3
        // Composite.

        material.EnableKeyword(StarGlow.CompositeTypes[compositeType]);
        material.SetColor(compositeColorID, color);
        material.SetTexture(compositeTexID, compositeTex);

        Graphics.Blit(source, destination, material, 4);

        // STEP:4
        // Close.

        material.DisableKeyword(StarGlow.CompositeTypes[compositeType]);

        RenderTexture.ReleaseTemporary(brightnessTex);
        RenderTexture.ReleaseTemporary(blurredTex1);
        RenderTexture.ReleaseTemporary(blurredTex2);
        RenderTexture.ReleaseTemporary(compositeTex);
    }

    #endregion Method
}