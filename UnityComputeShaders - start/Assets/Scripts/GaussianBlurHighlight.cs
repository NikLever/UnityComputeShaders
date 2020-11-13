using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GaussianBlurHighlight : BaseCompletePP
{
    [Range(0, 50)]
    public int blurRadius = 20;
    [Range(0.0f, 100.0f)]
    public float radius = 10;
    [Range(0.0f, 100.0f)]
    public float softenEdge = 30;
    [Range(0.0f, 1.0f)]
    public float shade = 0.5f;
    public Transform trackedObject;

    Vector4 center;
    ComputeBuffer weightsBuffer = null;

    RenderTexture horzOutput = null;
    int kernelHorzPassID;

    protected override void Init()
    {
        center = new Vector4();
        kernelName = "Highlight";
        kernelHorzPassID = shader.FindKernel("HorzPass");
        base.Init();

    }

    float[] SetWeightsArray(int radius, float sigma)
    {
        int total = radius * 2 + 1;
        float[] weights = new float[total];
        float sum = 0.0f;
        float c = 1 / Mathf.Sqrt(2 * Mathf.PI * sigma * sigma);

        for (int n=0; n<radius; n++)
        {
            float weight = c * Mathf.Exp(-0.5f * n * n / (sigma * sigma));
            weights[radius + n] = weight;
            weights[radius - n] = weight;
            if (n != 0)
                sum += weight * 2.0f;
            else
                sum += weight;
        }
        // normalize kernels
        for (int i=0; i<total; i++) weights[i] /= sum;
        
        return weights;
    }

    private void UpdateWeightsBuffer()
    {
        if (weightsBuffer != null)
            weightsBuffer.Dispose();

        float sigma = (float)blurRadius / 1.5f;

        weightsBuffer = new ComputeBuffer(blurRadius * 2 + 1, sizeof(float));
        float[] blurWeights = SetWeightsArray(blurRadius, sigma);
        weightsBuffer.SetData(blurWeights);

        shader.SetBuffer(kernelHorzPassID, "weights", weightsBuffer);
        shader.SetBuffer(kernelHandle, "weights", weightsBuffer);
    }

    protected override void CreateTextures()
    {
        base.CreateTextures();
        shader.SetTexture(kernelHorzPassID, "source", renderedSource);

        CreateTexture(ref horzOutput );

        shader.SetTexture(kernelHorzPassID, "horzOutput", horzOutput);
        shader.SetTexture(kernelHandle, "horzOutput", horzOutput);
    }

    private void OnValidate()
    {
        if(!init)
            Init();
           
        SetProperties();

        UpdateWeightsBuffer();
    }

    protected void SetProperties()
    {
        float rad = (radius / 100.0f) * texSize.y;
        shader.SetFloat("radius", rad);
        shader.SetFloat("edgeWidth", rad * softenEdge / 100.0f);
        shader.SetInt("blurRadius", blurRadius);
        shader.SetFloat("shade", shade);
    }

    protected override void DispatchWithSource(ref RenderTexture source, ref RenderTexture destination)
    {
        if (!init) return;

        Graphics.Blit(source, renderedSource);

        shader.Dispatch(kernelHorzPassID, groupSize.x, groupSize.y, 1);
        shader.Dispatch(kernelHandle, groupSize.x, groupSize.y, 1);

        Graphics.Blit(output, destination);
    }

    protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (shader == null)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            if (trackedObject && thisCamera)
            {
                Vector3 pos = thisCamera.WorldToScreenPoint(trackedObject.position);
                center.x = pos.x;
                center.y = pos.y;
                shader.SetVector("center", center);
            }
            bool resChange = false;
            CheckResolution(out resChange);
            if (resChange) SetProperties();
            DispatchWithSource(ref source, ref destination);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateWeightsBuffer();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (weightsBuffer != null) weightsBuffer.Dispose();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (weightsBuffer != null) weightsBuffer.Dispose();
    }
}
