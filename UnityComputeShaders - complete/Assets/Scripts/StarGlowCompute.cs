using System.Collections.Generic;
using UnityEngine;

public class StarGlowCompute : BasePP
{

    #region Field

    [Range(0, 1)]
    public float threshold = 1;

    [Range(0, 10)]
    public float intensity = 1;

    [Range(1, 20)]
    public int divide = 7;

    [Range(1, 5)]
    public int iteration = 5;

    [Range(0, 1)]
    public float attenuation = 1;

    [Range(0, 360)]
    public float streakAngle = 0;

    [Range(1, 16)]
    public int streakCount = 5;

    public Color color = Color.white;

    #endregion Field

    #region Method

	int kernelBrightnessBlurID;
    int kernelBlurID;
    int kernelCompositeID;
    int kernelFinalID;

    RenderTexture brightnessTexture;
    RenderTexture blur1Texture;
    RenderTexture blur2Texture;
    RenderTexture compositeTexture;

    Vector2Int groupSizeB = new Vector2Int();

    void Start()
    {
        Init();
    }

    protected override void Init()
    {
        kernelName = "BrightnessPass";
        kernelBrightnessBlurID = shader.FindKernel("BrightnessBlurPass");
        kernelBlurID = shader.FindKernel("BlurPass");
        kernelCompositeID = shader.FindKernel("CompositePass");
        kernelFinalID = shader.FindKernel("FinalPass");
        base.Init();
    }

    protected override void CreateTextures()
    {
        base.CreateTextures();
        
        CreateTexture(ref brightnessTexture, divide);
        CreateTexture(ref blur1Texture, divide);
        CreateTexture(ref blur2Texture, divide);
        CreateTexture(ref compositeTexture, divide);

        shader.SetTexture(kernelHandle, "brightnessTex", brightnessTexture);
        shader.SetTexture(kernelBrightnessBlurID, "brightnessTex", brightnessTexture);
        shader.SetTexture(kernelBrightnessBlurID, "blur1Tex", blur1Texture);
        shader.SetTexture(kernelBlurID, "blur1Tex", blur1Texture);
        shader.SetTexture(kernelBlurID, "blur2Tex", blur2Texture);
        shader.SetTexture(kernelCompositeID, "blur1Tex", blur1Texture);
        shader.SetTexture(kernelCompositeID, "compositeTex", compositeTexture);
        shader.SetTexture(kernelFinalID, "source", renderedSource);
        shader.SetTexture(kernelFinalID, "compositeTex", compositeTexture);
        shader.SetTexture(kernelFinalID, "output", output);
    }

    protected override void ClearTextures()
    {
        base.ClearTextures();

        ClearTexture(ref brightnessTexture);
        ClearTexture(ref blur1Texture);
        ClearTexture(ref blur2Texture);
        ClearTexture(ref compositeTexture);
    }

    private void OnValidate()
    {
        if (!init)
            Init();

        SetProperties();
    }

    protected void SetProperties()
    {
        shader.SetFloat("threshold", threshold);
        shader.SetFloat("intensity", intensity);
        shader.SetFloat("attenuation", attenuation);

        shader.SetInt("divide", divide);
        shader.SetInt("iteration", iteration);

        shader.SetInt("streakCount", streakCount);
        shader.SetFloat("streakAngle", streakAngle);

        shader.SetVector("color", color);

        groupSizeB.x = Mathf.CeilToInt((float)groupSize.x / (float)divide);
        groupSizeB.y = Mathf.CeilToInt((float)groupSize.y / (float)divide);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!init) return;

        //Copy the source to the renderedSource
        Graphics.Blit(source, renderedSource);

        // STEP:1
        // Set brightness image.
        shader.Dispatch(kernelHandle, groupSizeB.x, groupSizeB.y, 1);
        // DEBUG:
        //Graphics.Blit(brightnessTexture, destination);
        //return;

        // STEP:2
        // Set blurred brightness image.
        float angle = 360f / streakCount;
        Vector4 blurSettings = new Vector4();
		
        for (int x = 1; x <= streakCount; x++)
        {
            Vector2 offset =
            (Quaternion.AngleAxis(angle * x + streakAngle, Vector3.forward) * Vector2.down).normalized;
            blurSettings.x = offset.x;
            blurSettings.y = offset.y;
            blurSettings.z = 1;
			shader.Dispatch(kernelBrightnessBlurID, groupSizeB.x, groupSizeB.y, 1);
			//DEBUG:
			Graphics.Blit(blur1Texture, destination);
			return;
			
            for (int i = 2; i <= iteration; i++)
            {
                float power = Mathf.Pow(4, i - 1);
                blurSettings.z = power;
                blurSettings.x = offset.x * power;
                blurSettings.y = offset.y * power;
                shader.SetVector("blurSettings", blurSettings);
                shader.Dispatch(kernelBlurID, groupSizeB.x, groupSizeB.y, 1);
                //DEBUG
                //Graphics.Blit(blur2Texture, destination);
                //return;
            }

            //DEBUG
            //Graphics.Blit(blur1Texture, destination);
            //return;
            
            shader.Dispatch(kernelCompositeID, groupSizeB.x, groupSizeB.y, 1);
        }
        
        //DEBUG
        //Graphics.Blit(compositeTexture, destination);
        //return;

        // STEP:3
        // Composite.
        shader.Dispatch(kernelFinalID, groupSize.x, groupSize.y, 1);
        Graphics.Blit(output, destination);
    }

    #endregion Method
}
