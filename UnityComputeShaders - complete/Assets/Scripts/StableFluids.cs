using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StableFluids : MonoBehaviour
{
    struct FluidCell
    {
        public float density;
        public float d;
        public Vector2 velocity;
        public Vector2 v;
    };
    const int SIZE_FLUID_CELL = 6 * sizeof(float);

    public int resolution = 512;
    public float viscosity = 1e-6f;
    public float force = 300;
    public float exponent = 200;

    public Texture2D sourceImage;
    public ComputeShader shader;
    
    public Material material;

    Vector2 previousInput;
    ComputeBuffer fluidBuffer;

    int groupSize;

    int kernelAdvect;
    int kernelSwap;
    int kernelJacobi1;
    int kernelJacobi2;
    int kernelForce;
    int kernelProjectInit;
    int kernelProject;
    
    RenderTexture fluidRT1;
    RenderTexture fluidRT2;

    // Start is called before the first frame update
    void Start()
    {
        InitRenderTextures();
        InitBuffer();
        InitShader();
    }

    RenderTexture CreateRenderTexture(int width, int height)
    {
        var format = RenderTextureFormat.ARGBHalf;
        
        var rt = new RenderTexture(width, height, 0, format);
        rt.enableRandomWrite = true;
        rt.Create();

        return rt;
    }

    void InitRenderTextures()
    {
        fluidRT1 = CreateRenderTexture(Screen.width, Screen.height);
        fluidRT2 = CreateRenderTexture(Screen.width, Screen.height);
        Graphics.Blit( sourceImage, fluidRT1);
    }

    void InitBuffer()
    {
        int size = resolution * resolution;

        fluidBuffer = new ComputeBuffer(size, SIZE_FLUID_CELL);

        groupSize = Mathf.CeilToInt((float)size / 8.0f );
    }

    void InitShader()
    {
        kernelAdvect = shader.FindKernel("Advect");
        kernelSwap = shader.FindKernel("Swap");
        kernelJacobi1 = shader.FindKernel("Jacobi1");
        kernelJacobi2 = shader.FindKernel("Jacobi2");
        kernelForce = shader.FindKernel("Force");
        kernelProjectInit = shader.FindKernel("ProjectInit");
        kernelProject = shader.FindKernel("Project");

        shader.SetBuffer(kernelAdvect, "fluid", fluidBuffer);
        shader.SetBuffer(kernelSwap, "fluid", fluidBuffer);
        shader.SetBuffer(kernelJacobi1, "fluid", fluidBuffer);
        shader.SetBuffer(kernelJacobi2, "fluid", fluidBuffer);
        shader.SetBuffer(kernelForce, "fluid", fluidBuffer);
        shader.SetBuffer(kernelProjectInit, "fluid", fluidBuffer);
        shader.SetBuffer(kernelProject, "fluid", fluidBuffer);

        shader.SetInt("resolution", resolution);
        shader.SetInt("fluidMax", resolution * resolution);
        shader.SetFloat("forceExponent", exponent);

        material.SetBuffer("_FluidBuffer", fluidBuffer);
        material.SetFloat("_ForceExponent", exponent);
        material.SetInt("_Resolution", resolution);
    }

    // Update is called once per frame
    void Update()
    {
        float dx = 1.0f / resolution;

        // Input point
        Vector2 input = new Vector2(
            (Input.mousePosition.x - Screen.width * 0.5f) / Screen.height,
            (Input.mousePosition.y - Screen.height * 0.5f) / Screen.height
        );

        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);

        shader.Dispatch(kernelAdvect, groupSize, 1, 1);
        shader.SetInt("swapId", 0);
        shader.Dispatch(kernelSwap, groupSize, 1, 1);

        float diffAlpha = dx * dx / (viscosity * Time.deltaTime);
        shader.SetFloat("alpha", diffAlpha);
        shader.SetFloat("beta", 4 + diffAlpha);

        shader.SetInt("swapId", 6);

        // Jacobi iteration
        for (var i = 0; i < 20; i++)
        {
            shader.Dispatch(kernelJacobi2, groupSize, 1, 1);
            shader.Dispatch(kernelSwap, groupSize, 1, 1);
        }

        // Add external force
        shader.SetVector("forceOrigin", input);
        
        if (Input.GetMouseButton(1))
            // Random push
            shader.SetVector("forceVector", Random.insideUnitCircle * force * 0.025f);
        else if (Input.GetMouseButton(0))
            // Mouse drag
            shader.SetVector("forceVector", (input - previousInput) * force);
        else
            shader.SetVector("forceVector", Vector4.zero);

        shader.Dispatch(kernelForce, groupSize, 1, 1);

        // Projection setup
        shader.Dispatch(kernelProjectInit, groupSize, 1, 1);

        // Jacobi iteration
        shader.SetFloat("alpha", -dx * dx);
        shader.SetFloat("beta", 4);
        shader.SetInt("swapId", 1);
        
        for (var i = 0; i < 20; i++)
        {
            shader.Dispatch(kernelJacobi1, groupSize, 1, 1);
            shader.Dispatch(kernelSwap, groupSize, 1, 1);
        }

        // Projection finish
        shader.Dispatch(kernelProject, groupSize, 1, 1);

        var offs = Vector2.one * (Input.GetMouseButton(1) ? 0 : 1e+7f);
        material.SetVector("_ForceOrigin", input + offs);
        Graphics.Blit(fluidRT1, fluidRT2, material, 0);

        // Swap the color buffers.
        var temp = fluidRT1;
        fluidRT1 = fluidRT2;
        fluidRT2 = temp;

        previousInput = input;
    }

    private void OnDestroy()
    {
        fluidBuffer.Dispose();
        Destroy(fluidRT1);
        Destroy(fluidRT2);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit( fluidRT1, destination, material, 1);
    }
}
