using UnityEngine;
using System.Collections;

public class AssignTexture : MonoBehaviour
{

    public ComputeShader shader;
    public int texResolution = 256;

    Renderer rend;
    RenderTexture outputTexture;

    // Use this for initialization
    void Start()
    {
        outputTexture = new RenderTexture(texResolution, texResolution, 24);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        UpdateTextureFromCompute();
    }

    private void UpdateTextureFromCompute()
    {
        int kernelHandle = shader.FindKernel("CSMain");
        
        shader.SetTexture(kernelHandle, "Result", outputTexture);
        shader.Dispatch(kernelHandle, texResolution / 8, texResolution / 8, 1);

        rend.material.SetTexture("_MainTex", outputTexture);
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.U))
            UpdateTextureFromCompute();
    }
}

