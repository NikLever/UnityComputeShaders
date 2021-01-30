// StableFluids - Smoke

using UnityEngine;


public class Challenge7 : MonoBehaviour
{
    public int resolution = 512;
    public float viscosity = 1e-6f;
    public float force = 300;
    public float exponent = 200;
    public ComputeShader compute;
    public Shader shader;
    public Vector2 forceOrigin;
    public Vector2 forceVector;

    Material material;

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
        var format = RenderTextureFormat.ARGBHalf;
        if (componentCount == 1) format = RenderTextureFormat.RHalf;
        if (componentCount == 2) format = RenderTextureFormat.RGHalf;

        if (width == 0) width = resolutionX;
        if (height == 0) height = resolutionY;

        var rt = new RenderTexture(width, height, 0, format);
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
        material = new Material(shader);

        InitBuffers();
        InitShader();
    }

    void InitBuffers()
    {
        vfbRTV1 = CreateRenderTexture(2);
        vfbRTV2 = CreateRenderTexture(2);
        vfbRTV3 = CreateRenderTexture(2);
        vfbRTP1 = CreateRenderTexture(1);
        vfbRTP2 = CreateRenderTexture(1);

        colorRT1 = CreateRenderTexture(4, Screen.width, Screen.height);
        colorRT2 = CreateRenderTexture(4, Screen.width, Screen.height);
    }

    void InitShader()
    {
        kernelAdvect = compute.FindKernel("Advect");
        kernelForce = compute.FindKernel("Force");
        kernelProjectSetup = compute.FindKernel("ProjectSetup");
        kernelProject = compute.FindKernel("Project");
        kernelDiffuse1 = compute.FindKernel("Diffuse1");
        kernelDiffuse2 = compute.FindKernel("Diffuse2");

        compute.SetTexture(kernelAdvect, "U_in", vfbRTV1);
        compute.SetTexture(kernelAdvect, "W_out", vfbRTV2);

        compute.SetTexture(kernelDiffuse2, "B2_in", vfbRTV1);

        compute.SetTexture(kernelForce, "W_in", vfbRTV2);
        compute.SetTexture(kernelForce, "W_out", vfbRTV3);

        compute.SetTexture(kernelProjectSetup, "W_in", vfbRTV3);
        compute.SetTexture(kernelProjectSetup, "DivW_out", vfbRTV2);
        compute.SetTexture(kernelProjectSetup, "P_out", vfbRTP1);

        compute.SetTexture(kernelDiffuse1, "B1_in", vfbRTV2);

        compute.SetTexture(kernelProject, "W_in", vfbRTV3);
        compute.SetTexture(kernelProject, "P_in", vfbRTP1);
        compute.SetTexture(kernelProject, "U_out", vfbRTV1);
        compute.SetFloat("ForceExponent", exponent);

        // Input point
        Vector2 input = new Vector2(
            forceOrigin.x - 0.5f,
            forceOrigin.y - 0.5f
        );

        compute.SetVector("ForceOrigin", input);

        material.SetVector("_ForceOrigin", input);
        material.SetFloat("_ForceExponent", exponent);
        material.SetTexture("_VelocityField", vfbRTV1);

        Renderer rend = GetComponent<Renderer>();
        Material mat = rend.material;
        mat.SetTexture("_MainTex", colorRT1);
    }

    void OnDestroy()
    {
        Destroy(vfbRTV1);
        Destroy(vfbRTV2);
        Destroy(vfbRTV3);
        Destroy(vfbRTP1);
        Destroy(vfbRTP2);

        Destroy(colorRT1);
        Destroy(colorRT2);
    }

    void Update()
    {
        var dt = Time.deltaTime;
        var dx = 1.0f / resolutionY;

        // Common variables
        compute.SetFloat("Time", Time.time);
        compute.SetFloat("DeltaTime", dt);

        // Advection
        compute.Dispatch(kernelAdvect, threadCountX, threadCountY, 1);

        // Diffuse setup
        var difalpha = dx * dx / (viscosity * dt);
        compute.SetFloat("Alpha", difalpha);
        compute.SetFloat("Beta", 4 + difalpha);
        Graphics.CopyTexture(vfbRTV2, vfbRTV1);

        // Jacobi iteration
        for (var i = 0; i < 20; i++)
        {
            compute.SetTexture(kernelDiffuse2, "X2_in", vfbRTV2);
            compute.SetTexture(kernelDiffuse2, "X2_out", vfbRTV3);
            compute.Dispatch(kernelDiffuse2, threadCountX, threadCountY, 1);

            compute.SetTexture(kernelDiffuse2, "X2_in", vfbRTV3);
            compute.SetTexture(kernelDiffuse2, "X2_out", vfbRTV2);
            compute.Dispatch(kernelDiffuse2, threadCountX, threadCountY, 1);
        }

        //Add random vector
        Vector2 fV = forceVector * Random.value + new Vector2(Random.Range(-10f, 10f), Random.Range(-3,3));
        compute.SetVector("ForceVector", fV );

        // Add external force
        compute.Dispatch(kernelForce, threadCountX, threadCountY, 1);

        // Projection setup
        compute.Dispatch(kernelProjectSetup, threadCountX, threadCountY, 1);

        // Jacobi iteration
        compute.SetFloat("Alpha", -dx * dx);
        compute.SetFloat("Beta", 4);

        for (var i = 0; i < 20; i++)
        {
            compute.SetTexture(kernelDiffuse1, "X1_in", vfbRTP1);
            compute.SetTexture(kernelDiffuse1, "X1_out", vfbRTP2);
            compute.Dispatch(kernelDiffuse1, threadCountX, threadCountY, 1);

            compute.SetTexture(kernelDiffuse1, "X1_in", vfbRTP2);
            compute.SetTexture(kernelDiffuse1, "X1_out", vfbRTP1);
            compute.Dispatch(kernelDiffuse1, threadCountX, threadCountY, 1);
        }

        // Projection finish
        compute.Dispatch(kernelProject, threadCountX, threadCountY, 1);

        // Apply the velocity field to the color buffer.
        Graphics.Blit(colorRT1, colorRT2, material, 0);

        // Swap the color buffers.
        var temp = colorRT1;
        colorRT1 = colorRT2;
        colorRT2 = temp;
    }
}
