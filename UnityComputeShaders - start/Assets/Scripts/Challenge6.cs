using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Challenge6 : MonoBehaviour
{
    struct GrassClump
    {
        public Vector3 position;
        public float lean;
        public float trample;
        public Quaternion quaternion;
        public float noise;

        public GrassClump( Vector3 pos)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            lean = 0;
            noise = Random.Range(0.5f, 1);
            if (Random.value < 0.5f) noise = -noise;
            quaternion = Quaternion.identity;
            trample = 0;
        }
    }
    int SIZE_GRASS_CLUMP = 10 * sizeof(float);

    public Mesh mesh;
    public Material material;
    public Material visualizeNoise;
    public bool viewNoise = false;
    public ComputeShader shader;
    [Range(0,1)]
    public float density;
    [Range(0.1f,3)]
    public float scale;
    [Range(10, 45)]
    public float maxLean;
    public Transform trampler;
    [Range(0.1f,2)]
    public float trampleRadius = 0.5f;
    //TO DO: Add wind direction (0-360), speed (0-2)  and scale (10-1000)

    ComputeBuffer clumpsBuffer;
    ComputeBuffer argsBuffer;
    GrassClump[] clumpsArray;
    uint[] argsArray = new uint[] { 0, 0, 0, 0, 0 };
    Bounds bounds;
    int timeID;
    int tramplePosID;
    int groupSize;
    int kernelUpdateGrass;
    Vector4 pos = new Vector4();
    Material groundMaterial;

    // Start is called before the first frame update
    void Start()
    {
        bounds = new Bounds(Vector3.zero, new Vector3(30, 30, 30));

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        groundMaterial = renderer.material;
        
        InitShader();
    }

    private void OnValidate()
    {
        if (groundMaterial != null)
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();

            renderer.material = (viewNoise) ? visualizeNoise : groundMaterial;

            //TO DO: Set wind vector
            Vector4 wind = new Vector4();
            shader.SetVector("wind", wind);
            visualizeNoise.SetVector("wind", wind);
        }
    }

    void InitShader()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Bounds bounds = mf.sharedMesh.bounds;
        Vector2 size = new Vector2(bounds.extents.x * transform.localScale.x, bounds.extents.z * transform.localScale.z);
        
        Vector2 clumps = size;
        Vector3 vec = transform.localScale / 0.1f * density;
        clumps.x *= vec.x;
        clumps.y *= vec.z;

        int total = (int)clumps.x * (int)clumps.y;

        kernelUpdateGrass = shader.FindKernel("UpdateGrass");

        uint threadGroupSize;
        shader.GetKernelThreadGroupSizes(kernelUpdateGrass, out threadGroupSize, out _, out _);
        groupSize = Mathf.CeilToInt((float)total / (float)threadGroupSize);
        int count = groupSize * (int)threadGroupSize;

        clumpsArray = new GrassClump[count];

        for(int i=0; i<count; i++)
        {
            Vector3 pos = new Vector3(Random.value * size.x * 2 - size.x, 0, Random.value * size.y * 2 - size.y);
            clumpsArray[i] = new GrassClump(pos);
        }

        clumpsBuffer = new ComputeBuffer(count, SIZE_GRASS_CLUMP);
        clumpsBuffer.SetData(clumpsArray);

        shader.SetBuffer(kernelUpdateGrass, "clumpsBuffer", clumpsBuffer);
        shader.SetFloat("maxLean", maxLean * Mathf.PI / 180);
        shader.SetFloat("trampleRadius", trampleRadius);
        //TO DO: Set wind vector
        Vector4 wind = new Vector4();
        shader.SetVector("wind", wind);
        timeID = Shader.PropertyToID("time");
        tramplePosID = Shader.PropertyToID("tramplePos");

        argsArray[0] = mesh.GetIndexCount(0);
        argsArray[1] = (uint)count;
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);

        material.SetBuffer("clumpsBuffer", clumpsBuffer);
        material.SetFloat("_Scale", scale);

        visualizeNoise.SetVector("wind", wind);
    }

    // Update is called once per frame
    void Update()
    {
        shader.SetFloat(timeID, Time.time);
        pos = trampler.position;
        shader.SetVector(tramplePosID, pos);

        shader.Dispatch(kernelUpdateGrass, groupSize, 1, 1);

        if (!viewNoise) {
            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
        }
    }

    private void OnDestroy()
    {
        clumpsBuffer.Release();
        argsBuffer.Release();
    }
}
