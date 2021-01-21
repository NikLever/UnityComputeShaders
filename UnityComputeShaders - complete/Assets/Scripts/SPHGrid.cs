using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPHGrid : MonoBehaviour
{
    private struct SPHParticle
    {
        public Vector3 position;

        public Vector3 velocity;
        public Vector3 force;
        
        public float density;
        public float pressure;

        public Vector3Int gridLocation;
        public int gridIndex;

        public Vector3Int voxel;
        public int voxelW;

        public SPHParticle(Vector3 pos)
        {
            position = pos;
            velocity = Vector3.zero;
            force = Vector3.zero;
            density = 0.0f;
            pressure = 0.0f;
            gridLocation = Vector3Int.zero;
            gridIndex = 0;
            voxel = Vector3Int.zero;
            voxelW = 0;
        }
    }
    int SIZE_SPHPARTICLE = 11 * sizeof(float) + 8 * sizeof(int);

    private struct SPHCollider
    {
        public Vector3 position;
        public Vector3 right;
        public Vector3 up;
        public Vector2 scale;

        public SPHCollider(Transform _transform)
        {
            position = _transform.position;
            right = _transform.right;
            up = _transform.up;
            scale = new Vector2(_transform.lossyScale.x / 2f, _transform.lossyScale.y / 2f);
        }
    }
    int SIZE_SPHCOLLIDER = 11 * sizeof(float);

    public float particleRadius = 1;
    public float smoothingRadius = 1;
    public float restDensity = 15;
    public float particleMass = 0.1f;
    public float particleViscosity = 1;
    public float particleDrag = 0.025f;
    public Mesh particleMesh = null;
    public int particleCount = 5000;
    public int rowSize = 100;
    public ComputeShader shader;
    public Material material;
    public Transform gridBounds;
    public bool useGrid;
    public bool debug;

    // Consts
    private static Vector4 GRAVITY = new Vector4(0.0f, -9.81f, 0.0f, 2000.0f);
    private const float DT = 0.0008f;
    private const float BOUND_DAMPING = -0.5f;
    const float GAS = 2000.0f;
    const int SIZE_GRID_CELL = 4 * sizeof(int);

    private float smoothingRadiusSq;

    // Data
    SPHParticle[] particlesArray;
    ComputeBuffer particlesBuffer;
    SPHCollider[] collidersArray;
    ComputeBuffer collidersBuffer;
    uint[] argsArray = { 0, 0, 0, 0, 0 };
    ComputeBuffer argsBuffer;
    ComputeBuffer gridBuffer;
    float[] debugArray;
    ComputeBuffer debugBuffer;

    Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 0);

    int kernelClearGrid;
    int kernelPopulateGrid;
    int kernelComputeDensityPressure;
    int kernelComputeForces;
    int kernelIntegrate;
    int kernelComputeColliders;

    int groupSize;
    int gridGroupSize;
    int gridCount;
    Vector4 gridDimensions;
    Vector4 gridStartPosition;
    
    private void Start()
    {
        InitSPH();
        InitGrid();
        InitShader();
    }

    void UpdateColliders()
    {
        // Get colliders
        GameObject[] collidersGO = GameObject.FindGameObjectsWithTag("SPHCollider");
        if (collidersArray == null || collidersArray.Length != collidersGO.Length)
        {
            collidersArray = new SPHCollider[collidersGO.Length];
            if (collidersBuffer != null)
            {
                collidersBuffer.Dispose();
            }
            collidersBuffer = new ComputeBuffer(collidersArray.Length, SIZE_SPHCOLLIDER);
        }
        for (int i = 0; i < collidersArray.Length; i++)
        {
            collidersArray[i] = new SPHCollider(collidersGO[i].transform);
        }
        collidersBuffer.SetData(collidersArray);
        shader.SetBuffer(kernelComputeColliders, "colliders", collidersBuffer);
    }

    private void Update()
    {
        shader.SetInt("useGrid", useGrid ? 1 : 0);

        UpdateColliders();

        if (useGrid)
        {
            shader.Dispatch(kernelClearGrid, gridGroupSize, 1, 1);
            shader.Dispatch(kernelPopulateGrid, groupSize, 1, 1);
        }
        shader.Dispatch(kernelComputeDensityPressure, groupSize, 1, 1);
        shader.Dispatch(kernelComputeForces, groupSize, 1, 1);
        shader.Dispatch(kernelIntegrate, groupSize, 1, 1);
        shader.Dispatch(kernelComputeColliders, groupSize, 1, 1);
        

        if (debug)
        {
            particlesBuffer.GetData(particlesArray);
        }

        Graphics.DrawMeshInstancedIndirect(particleMesh, 0, material, bounds, argsBuffer);
    }

    void InitGrid()
    {
        gridDimensions.Set((int)gridBounds.localScale.x,
                            (int)gridBounds.localScale.y,
                            (int)gridBounds.localScale.z,
                            0);

        float cellSize = smoothingRadius;
        gridDimensions /= cellSize;
        gridDimensions.w = gridDimensions.x * gridDimensions.y * gridDimensions.z;

        Vector3 halfSize = new Vector3(gridDimensions.x, gridDimensions.y, gridDimensions.z) * cellSize * 0.5f;
        Vector3 pos = gridBounds.position - halfSize;
        gridStartPosition.Set(pos.x, pos.y, pos.z, cellSize);

        kernelClearGrid = shader.FindKernel("ClearGrid");

        uint numThreadsX;
        shader.GetKernelThreadGroupSizes(kernelClearGrid, out numThreadsX, out _, out _);
        gridGroupSize = Mathf.CeilToInt(gridDimensions.w / (float)numThreadsX);

        gridCount = (int)numThreadsX * gridGroupSize;

        Debug.Log("gridDimensions:" + gridDimensions);
        Debug.Log("gridStartPosition:" + gridStartPosition);
        
        gridBuffer = new ComputeBuffer(gridCount, SIZE_GRID_CELL);
    }

    void InitShader()
    {
        kernelPopulateGrid = shader.FindKernel("PopulateGrid");
        kernelComputeForces = shader.FindKernel("ComputeForces");
        kernelIntegrate = shader.FindKernel("Integrate");
        kernelComputeColliders = shader.FindKernel("ComputeColliders");

        float smoothingRadiusSq = smoothingRadius * smoothingRadius;

        particlesBuffer = new ComputeBuffer(particlesArray.Length, SIZE_SPHPARTICLE);
        particlesBuffer.SetData(particlesArray);

        UpdateColliders();

        argsArray[0] = particleMesh.GetIndexCount(0);
        argsArray[1] = (uint)particlesArray.Length;
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);

        debugArray = new float[25];
        debugBuffer = new ComputeBuffer(25, sizeof(float));
        debugBuffer.SetData(debugArray);
        shader.SetBuffer(kernelClearGrid, "debugBuffer", debugBuffer);
        shader.SetBuffer(kernelPopulateGrid, "debugBuffer", debugBuffer);
        shader.SetBuffer(kernelComputeDensityPressure, "debugBuffer", debugBuffer);
        shader.SetBuffer(kernelComputeForces, "debugBuffer", debugBuffer);

        shader.SetInt("particleCount", particlesArray.Length);
        shader.SetInt("colliderCount", collidersArray.Length);
        shader.SetInt("gridCount", gridCount);
        shader.SetFloat("smoothingRadius", smoothingRadius);
        shader.SetFloat("smoothingRadiusSq", smoothingRadiusSq);
        shader.SetFloat("gas", GAS);
        shader.SetFloat("restDensity", restDensity);
        shader.SetFloat("radius", particleRadius);
        shader.SetFloat("mass", particleMass);
        shader.SetFloat("particleDrag", particleDrag);
        shader.SetFloat("particleViscosity", particleViscosity);
        shader.SetFloat("damping", BOUND_DAMPING);
        shader.SetFloat("deltaTime", DT);
        shader.SetVector("gravity", GRAVITY);
        int[] gridDims = new int[] { (int)gridDimensions.x, (int)gridDimensions.y, (int)gridDimensions.z, (int)gridDimensions.w};
        shader.SetInts("gridDimensions", gridDims);
        shader.SetVector("gridStartPosition", gridStartPosition);
        shader.SetInt("debug", debug ? 1 : 0);

        shader.SetBuffer(kernelClearGrid, "grid", gridBuffer);
        shader.SetBuffer(kernelPopulateGrid, "grid", gridBuffer);
        shader.SetBuffer(kernelPopulateGrid, "particles", particlesBuffer);
        shader.SetBuffer(kernelComputeDensityPressure, "grid", gridBuffer);
        shader.SetBuffer(kernelComputeDensityPressure, "particles", particlesBuffer);
        shader.SetBuffer(kernelComputeForces, "grid", gridBuffer);
        shader.SetBuffer(kernelComputeForces, "particles", particlesBuffer);
        shader.SetBuffer(kernelIntegrate, "particles", particlesBuffer);
        shader.SetBuffer(kernelComputeColliders, "particles", particlesBuffer);
        shader.SetBuffer(kernelComputeColliders, "colliders", collidersBuffer);

        //material.SetBuffer("_Grid", gridBuffer);
        //material.SetVector("_GridDimensions", gridDimensions);
        //material.SetVector("_GridStartPosition", gridStartPosition);
        material.SetBuffer("particles", particlesBuffer);
        material.SetFloat("_Radius", particleRadius);
    }

    private void InitSPH()
    {
        kernelComputeDensityPressure = shader.FindKernel("ComputeDensityPressure");

        uint numThreadsX;
        shader.GetKernelThreadGroupSizes(kernelComputeDensityPressure, out numThreadsX, out _, out _);
        groupSize = Mathf.CeilToInt((float)particleCount / (float)numThreadsX);
        int amount = (int)numThreadsX * groupSize;

        particlesArray = new SPHParticle[amount];
        float size = particleRadius * 1.1f;
        float center = rowSize * 0.5f;

        for (int i = 0; i < amount; i++)
        {
            Vector3 pos = new Vector3();
            pos.x = (i % rowSize) + Random.Range(-0.1f, 0.1f) - center;
            pos.y = 2 + (float)((i / rowSize) / rowSize) * 1.1f;
            pos.z = ((i / rowSize) % rowSize) + Random.Range(-0.1f, 0.1f) - center;
            pos *= particleRadius;

            particlesArray[i] = new SPHParticle( pos );
        }
    }

    private void OnDestroy()
    {
        if (debugBuffer!=null) debugBuffer.Dispose();
        gridBuffer.Dispose();
        particlesBuffer.Dispose();
        collidersBuffer.Dispose();
        argsBuffer.Dispose();
    }
}
