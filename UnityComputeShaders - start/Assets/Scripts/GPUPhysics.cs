using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GPUPhysics : MonoBehaviour {
	struct RigidBody
    {
		public Vector3 position;
		public Quaternion quaternion;
		public Vector3 velocity;
		public Vector3 angularVelocity;
		public int particleIndex;
		public int particleCount;

		public RigidBody(Vector3 pos, int pIndex, int pCount)
        {
			position = pos;
			quaternion = Random.rotation;//Quaternion.identity;
			velocity = angularVelocity = Vector3.zero;
			particleIndex = pIndex;
			particleCount = pCount;
        }
    };

	int SIZE_RIGIDBODY = 13 * sizeof(float) + 2 * sizeof(int);

	struct Particle
    {
		public Vector3 position;
		public Vector3 velocity;
		public Vector3 force;
		public Vector3 localPosition;
		public Vector3 offsetPosition;

		public Particle(Vector3 pos)
        {
			position = velocity = force = offsetPosition = Vector3.zero;
			localPosition = pos;
        }
    };

	int SIZE_PARTICLE = 15 * sizeof(float);

	// set from editor
	public Mesh cubeMesh {
		get {
			return CjLib.PrimitiveMeshFactory.BoxFlatShaded();
		}
	}

	public ComputeShader shader;
	public Material cubeMaterial;
	public Bounds bounds;
	public float cubeMass;
	public float scale;
	public int particlesPerEdge;
	public float springCoefficient;
	public float dampingCoefficient;
	public float tangentialCoefficient;
	public float gravityCoefficient;
	public float frictionCoefficient;
	public float angularFrictionCoefficient;
	public float angularForceScalar;
	public float linearForceScalar;
	public Vector3Int gridSize = new Vector3Int(5, 5, 5);
	public Vector3 gridPosition;//centre of grid
	public bool useGrid = true;
	public int rigidBodyCount = 1000;
	[Range(1, 20)]
	public int stepsPerUpdate = 10;
	
	// calculated
	private Vector3 cubeScale;
	
	int particlesPerBody;
	float particleDiameter;

	RigidBody[] rigidBodiesArray;
	Particle[] particlesArray;
	
	ComputeBuffer rigidBodiesBuffer;
	ComputeBuffer particlesBuffer;
	private ComputeBuffer argsBuffer;
	private ComputeBuffer voxelGridBuffer;               
	
	private int kernelGenerateParticleValues;
	private int kernelClearGrid;
	private int kernelPopulateGrid;
	private int kernelCollisionDetectionWithGrid;
	private int kernelComputeMomenta;
	private int kernelComputePositionAndRotation;
	private int kernelCollisionDetection;

	private int groupsPerRigidBody;
	private int groupsPerParticle;
	private int groupsPerGridCell;
	private int deltaTimeID;

	int activeCount = 0;

	private int frameCounter;

	void Start() {

		InitArrays();

		InitRigidBodies();

		InitParticles();

		InitBuffers();

		InitShader();
		
		InitInstancing();
		
	}

	void InitArrays()
    {
		
	}

	void InitRigidBodies()
    {
		
	}

	void InitParticles()
    {
		
	}

	void InitBuffers()
    {
		
	}

	void InitShader()
	{
		deltaTimeID = Shader.PropertyToID("deltaTime");

		int[] gridDimensions = new int[] { gridSize.x, gridSize.y, gridSize.z };
		shader.SetInts("gridDimensions", gridDimensions);
		shader.SetInt("gridMax", gridSize.x * gridSize.y * gridSize.z);
		shader.SetInt("particlesPerRigidBody", particlesPerBody);
		shader.SetFloat("particleDiameter", particleDiameter);
		shader.SetFloat("springCoefficient", springCoefficient);
		shader.SetFloat("dampingCoefficient", dampingCoefficient);
		shader.SetFloat("frictionCoefficient", frictionCoefficient);
		shader.SetFloat("angularFrictionCoefficient", angularFrictionCoefficient);
		shader.SetFloat("gravityCoefficient", gravityCoefficient);
		shader.SetFloat("tangentialCoefficient", tangentialCoefficient);
		shader.SetFloat("angularForceScalar", angularForceScalar);
		shader.SetFloat("linearForceScalar", linearForceScalar);
		shader.SetFloat("particleMass", cubeMass / particlesPerBody);
		shader.SetInt("particleCount", rigidBodyCount * particlesPerBody);
		Vector3 halfSize = new Vector3(gridSize.x, gridSize.y, gridSize.z) * particleDiameter * 0.5f;
		Vector3 pos = gridPosition * particleDiameter - halfSize;
		shader.SetFloats("gridStartPosition", new float[] { pos.x, pos.y, pos.z });

		int particleCount = rigidBodyCount * particlesPerBody;
		// Get Kernels
		kernelGenerateParticleValues = shader.FindKernel("GenerateParticleValues");
		kernelClearGrid = shader.FindKernel("ClearGrid");
		kernelPopulateGrid = shader.FindKernel("PopulateGrid");
		kernelCollisionDetectionWithGrid = shader.FindKernel("CollisionDetectionWithGrid");
		kernelComputeMomenta = shader.FindKernel("ComputeMomenta");
		kernelComputePositionAndRotation = shader.FindKernel("ComputePositionAndRotation");
		kernelCollisionDetection = shader.FindKernel("CollisionDetection");

		// Count Thread Groups
		groupsPerRigidBody = Mathf.CeilToInt(rigidBodyCount / 8.0f);
		groupsPerParticle = Mathf.CeilToInt(particleCount / 8f);
		groupsPerGridCell = Mathf.CeilToInt((gridSize.x * gridSize.y * gridSize.z) / 8f);

		// Bind buffers

		// kernel 0 GenerateParticleValues
		shader.SetBuffer(kernelGenerateParticleValues, "rigidBodiesBuffer", rigidBodiesBuffer);
		shader.SetBuffer(kernelGenerateParticleValues, "particlesBuffer", particlesBuffer);

		// kernel 6 Collision Detection
		shader.SetBuffer(kernelCollisionDetection, "particlesBuffer", particlesBuffer);
		shader.SetBuffer(kernelCollisionDetection, "voxelGridBuffer", voxelGridBuffer);

		// kernel 4 Computation of Momenta
		shader.SetBuffer(kernelComputeMomenta, "rigidBodiesBuffer", rigidBodiesBuffer);
		shader.SetBuffer(kernelComputeMomenta, "particlesBuffer", particlesBuffer);

		// kernel 5 Compute Position and Rotation
		shader.SetBuffer(kernelComputePositionAndRotation, "rigidBodiesBuffer", rigidBodiesBuffer);		
	}

	void InitInstancing() {
		// Setup Indirect Renderer
		cubeMaterial.SetBuffer("rigidBodiesBuffer", rigidBodiesBuffer);

		uint[] args = new uint[] { cubeMesh.GetIndexCount(0), (uint)1, 0, 0, 0 };
		argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
		argsBuffer.SetData(args);
	}

	void Update() {
		if (activeCount<rigidBodyCount && frameCounter++ > 5) {
			activeCount++;
			frameCounter = 0;
			shader.SetInt("activeCount", activeCount);
			uint[] args = new uint[] { cubeMesh.GetIndexCount(0), (uint)activeCount, 0, 0, 0 };
			argsBuffer.SetData(args);
		}

		float dt = Time.deltaTime/stepsPerUpdate;
		shader.SetFloat(deltaTimeID, dt);

		for (int i=0; i<stepsPerUpdate; i++) {
			shader.Dispatch(kernelGenerateParticleValues, groupsPerRigidBody, 1, 1);
			if (useGrid)
			{
				shader.Dispatch(kernelClearGrid, groupsPerGridCell, 1, 1);
				shader.Dispatch(kernelPopulateGrid, groupsPerParticle, 1, 1);
				shader.Dispatch(kernelCollisionDetectionWithGrid, groupsPerParticle, 1, 1);
            }
            else
            {
				shader.Dispatch(kernelCollisionDetection, groupsPerParticle, 1, 1);
			}
			shader.Dispatch(kernelComputeMomenta, groupsPerRigidBody, 1, 1);
			shader.Dispatch(kernelComputePositionAndRotation, groupsPerRigidBody, 1, 1);
		}

        Graphics.DrawMeshInstancedIndirect(cubeMesh, 0, cubeMaterial, bounds, argsBuffer);
	}

	void OnDestroy() {
		rigidBodiesBuffer.Release();
		particlesBuffer.Release();

		voxelGridBuffer.Release();

		if (argsBuffer != null) {
			argsBuffer.Release();
		}
	}
}