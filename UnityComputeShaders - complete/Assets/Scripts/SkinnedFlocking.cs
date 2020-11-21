using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinnedFlocking : MonoBehaviour {
    public struct Boid
    {
        public Vector3 position;
        public Vector3 direction;
        public float noise_offset;
        public float speed;
        public float frame;
        public float next_frame;
        public float frame_interpolation;
        public float padding;
    }

    public ComputeShader shader;

    private SkinnedMeshRenderer boidSMR;
    public GameObject boidObject;
    private Animator animator;
    public AnimationClip animationClip;

    private int numOfFrames;
    public int boidsCount;
    public float spawnRadius;
    public Boid[] boidsArray;
    public Transform target;

    public Mesh boidMesh;

    private int kernelHandle;
    private ComputeBuffer boidsBuffer;
    private ComputeBuffer vertexAnimationBuffer;
    public Material boidMaterial;
    ComputeBuffer argsBuffer;
    MaterialPropertyBlock props;

    const int GROUP_SIZE = 256;

    void Start()
    {
        // Initialize the indirect draw args buffer.
        argsBuffer = new ComputeBuffer(
            1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments
        );

        argsBuffer.SetData(new uint[5] {
            boidMesh.GetIndexCount(0), (uint) boidsCount, 0, 0, 0
        });

        // This property block is used only for avoiding an instancing bug.
        props = new MaterialPropertyBlock();
        props.SetFloat("_UniqueID", Random.value);

        this.boidsArray = new Boid[this.boidsCount];
        this.kernelHandle = shader.FindKernel("CSMain");

        for (int i = 0; i < this.boidsCount; i++)
        {
            this.boidsArray[i] = this.CreateBoidData();
            this.boidsArray[i].noise_offset = Random.value * 1000.0f;
        }

        boidsBuffer = new ComputeBuffer(boidsCount, 48);
        boidsBuffer.SetData(this.boidsArray);

        GenerateSkinnedAnimationForGPUBuffer();
    }

    Boid CreateBoidData()
    {
        Boid boidData = new Boid();
        Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
        Quaternion rot = Quaternion.Slerp(transform.rotation, Random.rotation, 0.3f);
        boidData.position = pos;
        boidData.direction = rot.eulerAngles;

        return boidData;
    }

    public float RotationSpeed = 1f;
    public float BoidSpeed = 1f;
    public float NeighbourDistance = 1f;
    public float BoidSpeedVariation = 1f;
    public float BoidFrameSpeed = 10f;
    public bool FrameInterpolation = true;
    void Update()
    {
        shader.SetFloat("DeltaTime", Time.deltaTime);
        shader.SetFloat("RotationSpeed", RotationSpeed);
        shader.SetFloat("BoidSpeed", BoidSpeed);
        shader.SetFloat("BoidSpeedVariation", BoidSpeedVariation);
        shader.SetVector("FlockPosition", target.transform.position);
        shader.SetFloat("NeighbourDistance", NeighbourDistance);
        shader.SetFloat("BoidFrameSpeed", BoidFrameSpeed);
        shader.SetInt("boidsCount", boidsCount);
        shader.SetInt("numOfFrames", numOfFrames);
        shader.SetBuffer(this.kernelHandle, "boidsBuffer", boidsBuffer);
        shader.Dispatch(this.kernelHandle, this.boidsCount / GROUP_SIZE + 1, 1, 1);

        boidMaterial.SetBuffer("boidsBuffer", boidsBuffer);

        if (FrameInterpolation && !boidMaterial.IsKeywordEnabled("FRAME_INTERPOLATION"))
            boidMaterial.EnableKeyword("FRAME_INTERPOLATION");
        if (!FrameInterpolation && boidMaterial.IsKeywordEnabled("FRAME_INTERPOLATION"))
            boidMaterial.DisableKeyword("FRAME_INTERPOLATION");

        boidMaterial.SetInt("numOfFrames", numOfFrames);

        Graphics.DrawMeshInstancedIndirect(
            boidMesh, 0, boidMaterial,
            new Bounds(Vector3.zero, Vector3.one * 1000),
            argsBuffer, 0, props
        );
    }

    void OnDestroy()
    {
        if (boidsBuffer != null) boidsBuffer.Release();
        if (argsBuffer != null) argsBuffer.Release();
        if (vertexAnimationBuffer != null) vertexAnimationBuffer.Release();
    }

    private void GenerateSkinnedAnimationForGPUBuffer()
    {
        boidSMR = boidObject.GetComponentInChildren<SkinnedMeshRenderer>();
        animator = boidObject.GetComponentInChildren<Animator>();
        int iLayer = 0;
        AnimatorStateInfo aniStateInfo = animator.GetCurrentAnimatorStateInfo(iLayer);

        Mesh bakedMesh = new Mesh();
        float sampleTime = 0;
        float perFrameTime = 0;

        numOfFrames = Mathf.ClosestPowerOfTwo((int)(animationClip.frameRate * animationClip.length));
        perFrameTime = animationClip.length / numOfFrames;

        var vertexCount = boidSMR.sharedMesh.vertexCount;
        vertexAnimationBuffer = new ComputeBuffer(vertexCount * numOfFrames, 16);
        Vector4[] vertexAnimationData = new Vector4[vertexCount * numOfFrames];
        for (int i = 0; i < numOfFrames; i++)
        {
            animator.Play(aniStateInfo.shortNameHash, iLayer, sampleTime);
            animator.Update(0f);

            boidSMR.BakeMesh(bakedMesh);

            for(int j = 0; j < vertexCount; j++)
            {
                Vector3 vertex = bakedMesh.vertices[j];
                vertexAnimationData[(j * numOfFrames) +  i] = vertex;
            }

            sampleTime += perFrameTime;
        }

        vertexAnimationBuffer.SetData(vertexAnimationData);
        boidMaterial.SetBuffer("vertexAnimation", vertexAnimationBuffer);

        boidObject.SetActive(false);
    }
}
