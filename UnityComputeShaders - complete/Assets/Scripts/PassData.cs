using UnityEngine;
using System.Collections;

public class PassData : MonoBehaviour
{

    public ComputeShader shader;
    public int texResolution = 1024;

    Renderer rend;
    RenderTexture outputTexture;

    int clearHandle;
    int circlesHandle;

    public Color clearColor = new Color();
    public Color circleColor = new Color();

    // Use this for initialization
    void Start()
    {
        outputTexture = new RenderTexture(texResolution, texResolution, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        InitShader();
    }

    private void InitShader()
    {
        clearHandle = shader.FindKernel("Clear");
        circlesHandle = shader.FindKernel("Circles");

        shader.SetVector( "clearColor", clearColor );
        shader.SetVector( "circleColor", circleColor );
        shader.SetTexture( clearHandle, "Result", outputTexture);
        shader.SetTexture( circlesHandle, "Result", outputTexture);

        rend.material.SetTexture("_MainTex", outputTexture);

        DispatchKernels(10);
    }
 
    private void DispatchKernels(int count)
    {
        //shader.Dispatch(clearHandle, texResolution / 8, texResolution / 8, 1);
        shader.Dispatch(circlesHandle, count, 1, 1);
    }

    void Update()
    {
        
    }
}

