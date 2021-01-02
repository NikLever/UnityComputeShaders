using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StableFluids : MonoBehaviour
{
    public int resolution = 512;
    public float viscosity = 1e-6f;
    public float force = 300;
    public float exponent = 200;
    public Texture2D initial;
    public ComputeShader computeShader;
    public Shader renderShader;

    Material material;
    Vector2 previousInput;

    int kernelAdvect;
    int kernelForce;
    int kernelProject;
    int kernelDiffuse;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
