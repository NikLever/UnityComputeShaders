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
        
        public Boid(Vector3 pos, Vector3 dir, bool frameInterpolation){
        	position.x = pos.x;
        	position.y = pos.y;
        	position.z = pos.z;
        	direction.x = dir.x;
        	direction.y = dir.y;
        	direction.z = dir.z;
        	noise_offset = Random.value * 1000.0f;
        	speed = frame = next_frame = padding = 0;
        	frame_interpolation = (frameInterpolation) ? 1 : 0;
        }
    }

    public ComputeShader shader;

    private SkinnedMeshRenderer boidSMR;
    public GameObject boidObject;
    private Animator animator;
    public AnimationClip animationClip;

    private int numOfFrames;
    public int boidsCount;
    public float spawnRadius;
    public Transform target;
	public Material boidMaterial;
	public float rotationSpeed = 1f;
    public float boidSpeed = 1f;
    public float neighbourDistance = 1f;
    public float boidSpeedVariation = 1f;
    public float boidFrameSpeed = 10f;
    public bool frameInterpolation = true;
    
    public Mesh boidMesh;

    private int kernelHandle;
    private ComputeBuffer boidsBuffer;
    private ComputeBuffer vertexAnimationBuffer;
    ComputeBuffer argsBuffer;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    MaterialPropertyBlock props;
    int groupSizeX;
	Boid[] boidsArray;
	int numOfBoids;
	Bounds bounds;
    
    void Start()
    {
    	kernelHandle = shader.FindKernel("CSMain");

        uint x;
        shader.GetKernelThreadGroupSizes(kernelHandle, out x, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)boidsCount / (float)x);
        numOfBoids = groupSizeX * (int)x;

        bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        props = new MaterialPropertyBlock();
        props.SetFloat("_UniqueID", Random.value);

        InitBoids();
        GenerateSkinnedAnimationForGPUBuffer();
        InitShader();
    }

	private void InitBoids()
    {
        boidsArray = new Boid[numOfBoids];

        for (int i = 0; i < numOfBoids; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            Quaternion rot = Quaternion.Slerp(transform.rotation, Random.rotation, 0.3f);
            boidsArray[i] = new Boid(pos, rot.eulerAngles, frameInterpolation);
        }
    }

private void GenerateSkinnedAnimationForGPUBuffer()
    {
        boidSMR = boidObject.GetComponentInChildren<SkinnedMeshRenderer>();
        animator = boidObject.GetComponentInChildren<Animator>();
        int iLayer = 0;
        AnimatorStateInfo aniStateInfo = animator.GetCurrentAnimatorStateInfo(iLayer);

		//boidMesh = boidSMR.sharedMesh;
		
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
    
    void InitShader()
    {
        boidsBuffer = new ComputeBuffer(numOfBoids, 12 * sizeof(float));
        boidsBuffer.SetData(boidsArray);

        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        if (boidMesh != null)
        {
            args[0] = (uint)boidMesh.GetIndexCount(0);
            args[1] = (uint)numOfBoids;
        }
        argsBuffer.SetData(args);

        shader.SetFloat("rotationSpeed", rotationSpeed);
        shader.SetFloat("boidSpeed", boidSpeed);
        shader.SetFloat("boidSpeedVariation", boidSpeedVariation);
        shader.SetVector("flockPosition", target.transform.position);
        shader.SetFloat("neighbourDistance", neighbourDistance);
        shader.SetInt("boidsCount", numOfBoids);
        shader.SetFloat("boidFrameSpeed", boidFrameSpeed);
        shader.SetInt("numOfFrames", numOfFrames);
        
        shader.SetBuffer(kernelHandle, "boidsBuffer", boidsBuffer);
        boidMaterial.SetBuffer("boidsBuffer", boidsBuffer);
        
        if (frameInterpolation && !boidMaterial.IsKeywordEnabled("FRAME_INTERPOLATION"))
            boidMaterial.EnableKeyword("FRAME_INTERPOLATION");
        if (!frameInterpolation && boidMaterial.IsKeywordEnabled("FRAME_INTERPOLATION"))
            boidMaterial.DisableKeyword("FRAME_INTERPOLATION");
    }
    
    void Update()
    {
    	shader.SetFloat("time", Time.time);
		shader.SetFloat("deltaTime", Time.deltaTime);
        
        shader.Dispatch(kernelHandle, groupSizeX, 1, 1);

        Graphics.DrawMeshInstancedIndirect( boidMesh, 0, boidMaterial, bounds, argsBuffer);
    }

    void OnDestroy()
    {
        if (boidsBuffer != null) boidsBuffer.Release();
        if (argsBuffer != null) argsBuffer.Release();
        if (vertexAnimationBuffer != null) vertexAnimationBuffer.Release();
    }
}
