using UnityEngine;
using System.Collections;

public class Challenge1 : MonoBehaviour
{

    public ComputeShader shader;
    public int texResolution = 1024;

    Renderer rend;
    RenderTexture outputTexture;

    int kernelHandle;

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
        kernelHandle = shader.FindKernel("Square");

        int halfRes = texResolution >> 1;
        int quarterRes = texResolution >> 2;

        Vector4 rect = new Vector4( quarterRes, quarterRes, halfRes, halfRes );

        shader.SetVector( "rect", rect );
        shader.SetTexture(kernelHandle, "Result", outputTexture);
       
        rend.material.SetTexture("_MainTex", outputTexture);

        DispatchShader(texResolution / 8, texResolution / 8);
    }

    private void DispatchShader(int x, int y)
    {
        shader.Dispatch(kernelHandle, x, y, 1);
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.U))
        {
            DispatchShader(texResolution / 8, texResolution / 8);
        }
    }
}

