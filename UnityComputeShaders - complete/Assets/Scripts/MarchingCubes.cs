using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MarchingCubes : MonoBehaviour
{
    struct Triangle
    {
#pragma warning disable 649 // disable unassigned variable warning
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
#pragma warning restore 649 // restore unassigned variable warning
    }

    public ComputeShader shader;
    public float particleRadius = 0.5f;

    [Header("Voxel Settings")]
    public Transform bounds;
    public float voxelSize;
   
    // Buffers
    ComputeBuffer triangleBuffer;
    ComputeBuffer particlesBuffer;
    ComputeBuffer triCountBuffer;

    bool settingsUpdated;
    int kernelMarch = -1;
    int kernelMoveParticles = -1;
    int particlesGroupSize;
    Vector3Int marchGroupSize;
    int timeID;
    Mesh mesh;
    MeshFilter meshFilter;
    
    private void Awake()
    {
        if (kernelMarch == -1) kernelMarch = shader.FindKernel("March");
        if (kernelMoveParticles == -1) kernelMoveParticles = shader.FindKernel("MoveParticles");
        if (mesh == null)
        {
            meshFilter = GetComponent<MeshFilter>();
            mesh = new Mesh();
            meshFilter.sharedMesh = mesh;
        }
        if (triangleBuffer == null)
        {
            InitShader();
        }
    }

    private void OnEnable()
    {
        Awake();
    }

    void InitShader()
    {
        uint numThreads;

        shader.GetKernelThreadGroupSizes(kernelMoveParticles, out numThreads, out _, out _);

        Vector3 boundsSize = bounds.localScale;
        
        Vector3 numVoxelsPerAxis = boundsSize / voxelSize;
        marchGroupSize.Set(Mathf.CeilToInt(numVoxelsPerAxis.x / (float)numThreads),
                           Mathf.CeilToInt(numVoxelsPerAxis.y / (float)numThreads),
                           Mathf.CeilToInt(numVoxelsPerAxis.z / (float)numThreads));

        int particleCount = 2;
        particlesGroupSize = Mathf.CeilToInt((float)particleCount / (float)numThreads);

        Vector3Int numVoxelsPerAxisInt = new Vector3Int((int)(marchGroupSize.x * numThreads),
                                                         (int)(marchGroupSize.y * numThreads),
                                                         (int)(marchGroupSize.z * numThreads));
        int voxelCount = numVoxelsPerAxisInt.x * numVoxelsPerAxisInt.y * numVoxelsPerAxisInt.z;

        CreateBuffers(particleCount, voxelCount);

        timeID = Shader.PropertyToID("time");

        triangleBuffer.SetCounterValue(0);
        shader.SetBuffer(kernelMoveParticles, "particles", particlesBuffer);
        shader.SetBuffer(kernelMarch, "particles", particlesBuffer);
        shader.SetBuffer(kernelMarch, "triangles", triangleBuffer);
        shader.SetVector("halfBoundsSize", boundsSize * 0.5f);
        shader.SetFloat("voxelSize", voxelSize);
        shader.SetFloat("particleRadius", particleRadius);
        shader.SetInt("particleCount", particleCount);
        int[] gridDimensions = new int[] { numVoxelsPerAxisInt.x, numVoxelsPerAxisInt.y, numVoxelsPerAxisInt.z, voxelCount };
        shader.SetInts("gridDimensions", gridDimensions);
    }

    void CreateBuffers(int particleCount, int voxelCount)
    {
        int maxTriangleCount = voxelCount * 5;

        // Always create buffers in editor (since buffers are released immediately to prevent memory leak)
        // Otherwise, only create if null or if size has changed
        if (!Application.isPlaying || (particlesBuffer == null || particleCount != particlesBuffer.count))
        {
            if (Application.isPlaying)
            {
                ReleaseBuffers();
            }

            Vector4[] particlesArray = new Vector4[particleCount];
            for(int i=0; i<particleCount; i++)
            {
                particlesArray[i].w = i * Mathf.PI;
            }

            triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
            particlesBuffer = new ComputeBuffer(particleCount, sizeof(float) * 4);
            particlesBuffer.SetData(particlesArray);
            triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        }
    }

    void Update()
    {
        if (settingsUpdated)
        {
            InitShader();
            settingsUpdated = false;
        }

        shader.SetFloat(timeID, Time.time);
        shader.Dispatch(kernelMoveParticles, particlesGroupSize, 1, 1);
        shader.Dispatch(kernelMarch, marchGroupSize.x, marchGroupSize.y, marchGroupSize.z);

        UpdateMesh();
    }

    void UpdateMesh()
    {
        
        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];
        triangleBuffer.SetCounterValue(0);

        // Get triangle data from shader
        Triangle[] tris = new Triangle[numTris];
        triangleBuffer.GetData(tris, 0, 0, numTris);

        mesh.Clear();

        var vertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];

        for (int i = 0; i < numTris; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = tris[i][j];
            }
        }
        mesh.vertices = vertices;
        mesh.triangles = meshTriangles;

        mesh.RecalculateNormals();
    }

    void OnDestroy()
    {
        if (Application.isPlaying)
        {
            ReleaseBuffers();
        }
    }

    void ReleaseBuffers()
    {
        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
            particlesBuffer.Release();
            triCountBuffer.Release();
        }
    }

    void OnValidate()
    {
        settingsUpdated = true;
    }

}