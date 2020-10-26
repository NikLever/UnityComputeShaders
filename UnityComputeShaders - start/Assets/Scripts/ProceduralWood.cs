using UnityEngine;
using System.Collections;

public class ProceduralWood : MonoBehaviour
{

    public ComputeShader shader;
    public int texResolution = 256;

    Renderer rend;
    RenderTexture outputTexture;

    int kernelHandle;

    public Color paleColor = new Color(0.733f, 0.565f, 0.365f, 1);
    public Color darkColor = new Color(0.49f, 0.286f, 0.043f, 1);
    public float frequency = 2.0f;
    public float noiseScale = 6.0f;
    public float ringScale = 0.6f;
    public float contrast = 4.0f;

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

        shader.SetVector("paleColor", paleColor);
        shader.SetVector("darkColor", darkColor);
        shader.SetFloat("frequency", frequency);
        shader.SetFloat("noiseScale", noiseScale);
        shader.SetFloat("ringScale", ringScale);
        shader.SetFloat("contrast", contrast);

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

