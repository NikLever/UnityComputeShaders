using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GaussianBlur : BlurCore
{
    private ComputeBuffer weightsBuffer;

     protected override void Init()
    {
        base.Init();
        base.CheckForKernels();
        init = true;
    }

    float[] OneDimensionalKernel(int radius, float sigma)
    {
        float[] kernelResult = new float[radius * 2 + 1];
        float sum = 0.0f;
        for(int t = 0; t< radius; t++)
        {
            double newBlurWalue = 0.39894 * Mathf.Exp(-0.5f * t*t / (sigma * sigma)) / sigma;
            kernelResult[radius+t] = (float)newBlurWalue;
            kernelResult[radius-t] = (float)newBlurWalue;
            if(t!=0)
                sum += (float)newBlurWalue*2.0f;
            else
                sum += (float)newBlurWalue;
        }
        // normalize kernels
        for(int k = 0; k< radius*2 +1; k++)
        {
            kernelResult[k]/=sum;
        }
        return kernelResult;
    }

    private void CalculateWeights()
    {
   
        if (weightsBuffer != null)
            weightsBuffer.Dispose();

        float sigma = ((int)radius)/1.5f;

        weightsBuffer = new ComputeBuffer((int)radius * 2 + 1, sizeof(float));
        float[] blurWeights = OneDimensionalKernel((int)radius,sigma);
        weightsBuffer.SetData(blurWeights);

        shader.SetBuffer(kernelHorzHandle, "gWeights", weightsBuffer);
        shader.SetBuffer(kernelVertHandle, "gWeights", weightsBuffer);
        shader.SetInt("blurRadius", (int)radius);

        // if you want to console write calculated values         
        foreach (float blurValue in blurWeights)
            Debug.Log(blurValue);

    }

    private void OnValidate()
    {
        if(!init)
        {
            base.Init();
            init = true;
        }
            
        SetRadius();
        CalculateWeights();
    }


    protected virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (radius < 0.5f || shader == null)
        { 
             Graphics.Blit(source, destination); // just copy
            return;
        }  
        CheckForErrorsAndResolution();
        DispatchWithSource(ref source, ref destination);
    }
}
