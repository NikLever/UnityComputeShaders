using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class BlurCore : MonoBehaviour
{
#if UNITY_IOS || UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    protected const int gpuMemoryBlockSizeBlur = 484;
    protected const int maxRadius = 64;
#elif UNITY_ANDROID
    protected const int gpuMemoryBlockSizeBlur = 64;
    protected const int maxRadius = 32;
#else
    protected const int gpuMemoryBlockSizeBlur = 1024;
    protected const int maxRadius = 92;
#endif

    protected private bool init = false;

    [Range(0.01f, 1.0f)]
    public float screenScaling = 1.0f;

    [Range(0.0f, maxRadius)]
    public float radius = 1;

    public ComputeShader shader;

    protected Vector2Int texSize = new Vector2Int(0,0);
    protected Vector2Int blockSize = new Vector2Int();
    protected Camera thisCamera;

    protected RenderTexture vertBlurOutput = null;
    protected RenderTexture horzBlurOutput = null;
    protected RenderTexture tempSource = null;

    protected private int kernelHorzHandle;
    protected private int kernelVertHandle;

    protected List<int> kernelsList = new List<int>();

    void ReportComputeShaderError()
    {
        Debug.LogError("Error in compute shader");
    }

    protected void CheckForErrorsAndResolution()
    {
        if (!shader)
        {
            init = false;
            ClearBuffers();
            ClearTextures();
            return;
        }

        texSize.x = Mathf.RoundToInt(thisCamera.pixelWidth * screenScaling);
        texSize.y = Mathf.RoundToInt(thisCamera.pixelHeight * screenScaling);

        CheckForKernels();

        if (texSize.x != thisCamera.pixelWidth || texSize.y != thisCamera.pixelHeight)
        {
            CreateTextures();
        }
    }

    public void ClearTexture(ref RenderTexture textureToClear)
    {
        if (null != textureToClear)
        {
            textureToClear.Release();
            textureToClear = null;
        }
    }

    protected virtual void ClearBuffers()
    {

    }

    protected void ClearTextures()
    {
        ClearTexture(ref vertBlurOutput);
        ClearTexture(ref horzBlurOutput);
        ClearTexture(ref tempSource);
    }

    public void CreateTexture(ref RenderTexture textureToMake)
    {
        textureToMake = new RenderTexture(texSize.x, texSize.y, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        textureToMake.enableRandomWrite = true;
        textureToMake.wrapMode = TextureWrapMode.Clamp;
        textureToMake.Create();
    }


    protected void CreateTextures()
    {

        texSize.x = Mathf.RoundToInt(thisCamera.pixelWidth * screenScaling);
        texSize.y = Mathf.RoundToInt(thisCamera.pixelHeight * screenScaling);

        CreateTexture(ref vertBlurOutput);
        CreateTexture(ref horzBlurOutput);
        CreateTexture(ref tempSource);

        shader.SetTexture(kernelHorzHandle, "source", tempSource);
        shader.SetTexture(kernelHorzHandle, "horzBlurOutput", horzBlurOutput);

        shader.SetTexture(kernelVertHandle, "horzBlurOutput", horzBlurOutput);
        shader.SetTexture(kernelVertHandle, "vertBlurOutput", vertBlurOutput);
    }

    protected void SetRadius()
    {
        shader.SetInt("blurRadius", (int)radius);
    }

    protected virtual void Init()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError(" It seems your target Hardware does not support Compute Shaders.");
            return;
        }

        if (!shader)
        {
            Debug.LogError("No shader");
            return;
        }

        kernelHorzHandle = shader.FindKernel("HorzBlur");
        kernelVertHandle = shader.FindKernel("VertBlur");
        kernelsList.Add(kernelHorzHandle);
        kernelsList.Add(kernelVertHandle);

        SetRadius();

        thisCamera = GetComponent<Camera>();

        if (!thisCamera)
        {
            Debug.LogError("Object has no Camera");
            return;
        }

        CreateTextures();

    }

    protected void CheckForKernels()
    {
        foreach (int kernel in kernelsList)
        {
            if (kernel < 0)
            {
                ReportComputeShaderError();
            }
        }
    }

    private void OnEnable()
    {
        OnBlurEnable();
    }

    private void OnDisable()
    {
        OnBlurDisable();
    }

    private void OnDestroy()
    {
        OnBlurDestroy();
    }

    protected virtual void OnBlurEnable()
    {
        kernelsList = new List<int>();
        Init();
        CreateTextures();
        init = true;
    }

    protected virtual void OnBlurDisable()
    {
        ClearTextures();
        ClearBuffers();
        init = false;
    }

    protected virtual void OnBlurDestroy()
    {
        ClearTextures();
        ClearBuffers();
        init = false;
    }

    protected void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        if (!init) return;

        int horizontalBlurDisX = Mathf.CeilToInt(((float)texSize.x / (float)gpuMemoryBlockSizeBlur)); // it is here becouse res of window can change
        int horizontalBlurDisY = Mathf.CeilToInt(((float)texSize.y / (float)gpuMemoryBlockSizeBlur));

        Graphics.Blit(source, tempSource);

        shader.Dispatch(kernelHorzHandle, horizontalBlurDisX, texSize.y, 1);
        shader.Dispatch(kernelVertHandle, texSize.x, horizontalBlurDisY, 1);

        Graphics.Blit(vertBlurOutput, destination);
    }

    protected void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination, Material postProcessMat)
    {
        if (!init)
            return;
        int horizontalBlurDisX = Mathf.CeilToInt(((float)texSize.x / (float)gpuMemoryBlockSizeBlur)); // it is here becouse res of window can change
        int horizontalBlurDisY = Mathf.CeilToInt(((float)texSize.y / (float)gpuMemoryBlockSizeBlur));

        Graphics.Blit(source, tempSource, postProcessMat);
        shader.Dispatch(kernelHorzHandle, horizontalBlurDisX, texSize.x, 1);
        shader.Dispatch(kernelVertHandle, texSize.y, horizontalBlurDisY, 1);

        Graphics.Blit(vertBlurOutput, destination, postProcessMat);
    }

}
