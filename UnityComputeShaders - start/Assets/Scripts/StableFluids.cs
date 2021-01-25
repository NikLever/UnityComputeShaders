// StableFluids - A GPU implementation of Jos Stam's Stable Fluids on Unity
// adapted from https://github.com/keijiro/StableFluids

using UnityEngine;


public class StableFluids : MonoBehaviour
{
    public int resolution = 512;
    public float viscosity = 1e-6f;
    public float force = 300;
    public float exponent = 200;
    public Texture2D initial;
    public ComputeShader compute;
    public Material material;
   
    Vector2 previousInput;

    int kernelAdvect;
    int kernelForce;
    int kernelProjectSetup;
    int kernelProject;
    int kernelDiffuse1;
    int kernelDiffuse2;

    int threadCountX { get { return (resolution + 7) / 8; } }
    int threadCountY { get { return (resolution * Screen.height / Screen.width + 7) / 8; } }

    int resolutionX { get { return threadCountX * 8; } }
    int resolutionY { get { return threadCountY * 8; } }

    // Vector field buffers
    RenderTexture vfbRTV1;
    RenderTexture vfbRTV2;
    RenderTexture vfbRTV3;
    RenderTexture vfbRTP1;
    RenderTexture vfbRTP2;

    // Color buffers (for double buffering)
    RenderTexture colorRT1;
    RenderTexture colorRT2;

    RenderTexture CreateRenderTexture(int componentCount, int width = 0, int height = 0)
    {
        RenderTexture rt = new RenderTexture(width, height, 0);
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;
    }

    
    void OnValidate()
    {
        resolution = Mathf.Max(resolution, 8);
    }

    void Start()
    {
        InitBuffers();
        InitShader();

        Graphics.Blit(initial, colorRT1);
    }

    void InitBuffers()
    {
        
    }

    void InitShader()
    {
        
    }

    void OnDestroy()
    {
        
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float dx = 1.0f / resolutionY;

        // Input point
        Vector2 input = new Vector2(
            (Input.mousePosition.x - Screen.width * 0.5f) / Screen.height,
            (Input.mousePosition.y - Screen.height * 0.5f) / Screen.height
        );

        // Common variables
        compute.SetFloat("Time", Time.time);
        compute.SetFloat("DeltaTime", dt);

        //Add code here



        previousInput = input;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(colorRT1, destination, material, 1);
    }
}
