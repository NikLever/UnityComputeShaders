using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassBlades : MonoBehaviour
{
    struct GrassBlade
    {
        public Vector3 position;
        public float bend;
        public float noise;
        public float fade;

        public GrassBlade( Vector3 pos)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            bend = 0;
            noise = Random.Range(0.5f, 1) * 2 - 1;
            fade = Random.Range(0.5f, 1);
        }
    }
    int SIZE_GRASS_BLADE = 6 * sizeof(float);

    public Material material;
    public ComputeShader shader;
    public Material visualizeNoise;
    public bool viewNoise = false;
    [Range(0,1)]
    public float density;
    [Range(0.1f,3)]
    public float scale;
    [Range(10, 45)]
    public float maxBend;
    [Range(0, 2)]
    public float windSpeed;
    [Range(0, 360)]
    public float windDirection;
    [Range(10, 1000)]
    public float windScale;

    ComputeBuffer bladesBuffer;
    ComputeBuffer argsBuffer;
    GrassBlade[] bladesArray;
    uint[] argsArray = new uint[] { 0, 0, 0, 0, 0 };
    Bounds bounds;
    int timeID;
    int groupSize;
    int kernelBendGrass;
    Mesh blade;
    Material groundMaterial;

    Mesh Blade
    {
        get
        {
            Mesh mesh;

            if (blade != null)
            {
                mesh = blade;
            }
            else
            {
                mesh = new Mesh();

                float height = 0.2f;
                float rowHeight = height / 4;
                float halfWidth = height / 10;

                Vector3[] vertices =
                {
                    new Vector3(-halfWidth, 0, 0),
                    new Vector3( halfWidth, 0, 0),
                    new Vector3(-halfWidth, rowHeight, 0),
                    new Vector3( halfWidth, rowHeight, 0),
                    new Vector3(-halfWidth*0.9f, rowHeight*2, 0),
                    new Vector3( halfWidth*0.9f, rowHeight*2, 0),
                    new Vector3(-halfWidth*0.8f, rowHeight*3, 0),
                    new Vector3( halfWidth*0.8f, rowHeight*3, 0),
                    new Vector3( 0, rowHeight*4, 0)
                };

                Vector3 normal = new Vector3(0, 0, -1);

                Vector3[] normals =
                {
                    normal,
                    normal,
                    normal,
                    normal,
                    normal,
                    normal,
                    normal,
                    normal,
                    normal
                };

                Vector2[] uvs =
                {
                    new Vector2(0,0),
                    new Vector2(1,0),
                    new Vector2(0,0.25f),
                    new Vector2(1,0.25f),
                    new Vector2(0,0.5f),
                    new Vector2(1,0.5f),
                    new Vector2(0,0.75f),
                    new Vector2(1,0.75f),
                    new Vector2(0.5f,1)
                };

                int[] indices =
                {
                    0,1,2,1,3,2,//row 1
                    2,3,4,3,5,4,//row 2
                    4,5,6,5,7,6,//row 3
                    6,7,8//row 4
                };

                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.uv = uvs;
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            }

            return mesh;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        bounds = new Bounds(Vector3.zero, new Vector3(30, 30, 30));
        blade = Blade;

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

            float theta = windDirection * Mathf.PI / 180;
            Vector4 wind = new Vector4(Mathf.Cos(theta), Mathf.Sin(theta), windSpeed, windScale);
            shader.SetVector("wind", wind);
            visualizeNoise.SetVector("wind", wind);
        }
    }

    void InitShader()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Bounds bounds = mf.sharedMesh.bounds;

        Vector3 blades = bounds.extents;
        Vector3 vec = transform.localScale / 0.1f * density;
        blades.x *= vec.x;
        blades.z *= vec.z;

        int total = (int)blades.x * (int)blades.z * 20;

        kernelBendGrass = shader.FindKernel("BendGrass");

        uint threadGroupSize;
        shader.GetKernelThreadGroupSizes(kernelBendGrass, out threadGroupSize, out _, out _);
        groupSize = Mathf.CeilToInt((float)total / (float)threadGroupSize);
        int count = groupSize * (int)threadGroupSize;

        bladesArray = new GrassBlade[count];

        for(int i=0; i<count; i++)
        {
            Vector3 pos = new Vector3( Random.value * bounds.extents.x * 2 - bounds.extents.x + bounds.center.x,
                                       0,
                                       Random.value * bounds.extents.z * 2 - bounds.extents.z + bounds.center.z);
            pos = transform.TransformPoint(pos);
            bladesArray[i] = new GrassBlade(pos);
        }

        bladesBuffer = new ComputeBuffer(count, SIZE_GRASS_BLADE);
        bladesBuffer.SetData(bladesArray);

        shader.SetBuffer(kernelBendGrass, "bladesBuffer", bladesBuffer);
        shader.SetFloat("maxBend", maxBend * Mathf.PI / 180);
        float theta = windDirection * Mathf.PI / 180;
        Vector4 wind = new Vector4(Mathf.Cos(theta), Mathf.Sin(theta), windSpeed, windScale);
        shader.SetVector("wind", wind);
        timeID = Shader.PropertyToID("time");

        argsArray[0] = blade.GetIndexCount(0);
        argsArray[1] = (uint)count;
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);

        material.SetBuffer("bladesBuffer", bladesBuffer);
        material.SetFloat("_Scale", scale);
    }

    // Update is called once per frame
    void Update()
    {
        shader.SetFloat(timeID, Time.time);
        shader.Dispatch(kernelBendGrass, groupSize, 1, 1);

        if (!viewNoise)
        {
            Graphics.DrawMeshInstancedIndirect(blade, 0, material, bounds, argsBuffer);
        }
    }

    private void OnDestroy()
    {
        bladesBuffer.Release();
        argsBuffer.Release();
    }
}
