using UnityEngine;
using System.Collections;

public class ProceduralMarble : MonoBehaviour
{

    public ComputeShader shader;
    public int texResolution = 256;

    Renderer rend;
    RenderTexture outputTexture;

    int kernelHandle;
    bool marble = true;

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
        kernelHandle = shader.FindKernel("CSMain");

        shader.SetInt("texResolution", texResolution);
        shader.SetTexture(kernelHandle, "Result", outputTexture);

        rend.material.SetTexture("_MainTex", outputTexture);

        shader.SetBool("marble", marble);
        marble = !marble;

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
            shader.SetBool("marble", marble);
            marble = !marble;
            DispatchShader(texResolution / 8, texResolution / 8);
        }
    }
}

