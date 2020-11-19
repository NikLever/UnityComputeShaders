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
    public int numOfStreak = 4;

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
        Graphics.Blit(source, destination);
    }

    #endregion Method
}