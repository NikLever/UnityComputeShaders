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
			quaternion = Quaternion.identity;
			velocity = angularVelocity = Vector3.zero;
			particleIndex = pIndex;
			particleCount = pCount;
        }
    };

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

	public bool m_debugWireframe;
	private bool m_lastDebugWireframe;
	// set from editor
	public ComputeShader shader;
	public Mesh cubeMesh {
		get {
			return m_debugWireframe ? CjLib.PrimitiveMeshFactory.BoxWireframe() : CjLib.PrimitiveMeshFactory.BoxFlatShaded();
		}
	}
	public Mesh sphereMesh {
		get {
			return CjLib.PrimitiveMeshFactory.SphereWireframe(6, 6);
		}
	}
	public Mesh lineMesh {
		get {
			return CjLib.PrimitiveMeshFactory.Line(Vector3.zero, new Vector3(1.0f, 1.0f, 1.0f));
		}
	}

	Rigidbody[] comparisonCubes;

	public Material cubeMaterial;
	public Material sphereMaterial;
	public Material lineMaterial;
	public Material lineAngularMaterial;
	public Bounds m_bounds;
	public float m_cubeMass;
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
	public Vector3 gridStartPosition;
	public int rigidBodyCount = 1000;
	public float tick_rate;
	private float ticker;

	
	// calculated
	private Vector3 m_cubeScale;
	
	// data

	private ComputeBuffer m_rigidBodyPositions;                 // float3
	private ComputeBuffer m_previousRigidBodyPositions;			// float3
	private ComputeBuffer m_rigidBodyQuaternions;               // float4
	private ComputeBuffer m_previousRigidBodyQuaternions;		// float4
	private ComputeBuffer m_rigidBodyAngularVelocities;         // float3
	private ComputeBuffer m_rigidBodyVelocities;                // float3
	//private ComputeBuffer m_rigidBodyInertialTensors;			// Matrix4x4
	private ComputeBuffer m_particleInitialRelativePositions;   // float3
	private ComputeBuffer m_particlePositions;                  // float3
	private ComputeBuffer m_particleRelativePositions;          // float3
	private ComputeBuffer m_particleVelocities;                 // float3
	private ComputeBuffer m_particleForces;                     // float3
	
	//private ComputeBuffer m_debugParticleIds;                   // int
	//private ComputeBuffer m_debugParticleVoxelPositions;        // int3 // the per particle grid locations
	private ComputeBuffer m_voxelCollisionGrid;                 // int4

	private CommandBuffer m_commandBuffer;

	int particlesPerBody;
	float particleDiameter;

	RigidBody[] rigidBodiesArray;
	Particle[] particlesArray;
	CommandBuffer rigidBodiesBuffer;
	CommandBuffer particlesBuffer;

	Vector3[] positionArray;                         // cpu->matrix
	Quaternion[] quaternionArray;                        // cpu->matrix
	Vector3[] particleForcesArray;
	Vector3[] particleVelocities;
	Vector3[] rigidBodyVelocitiesArray;
	Vector3[] particlePositions;
	Vector3[] particleRelativePositions;
	Vector3[] particleInitialRelativePositions;
	//public float[] rigidBodyInertialTensorsArray;

	int[] voxelGridArray;
	int[] particleVoxelPositionsArray;
	int[] debugParticleIds;

	private int kernelGenerateParticleValues;
	private int kernelClearGrid;
	private int kernelPopulateGrid;
	private int kernelCollisionDetection;
	private int kernelComputeMomenta;
	private int kernelComputePositionAndRotation;
	private int m_kernelSavePreviousPositionAndRotation;

	private int m_threadGroupsPerRigidBody;
	private int m_threadGroupsPerParticle;
	private int m_threadGroupsPerGridCell;
	private int deltaTimeID;

	
	private ComputeBuffer m_bufferWithArgs;
	private ComputeBuffer m_bufferWithSphereArgs;
	private ComputeBuffer m_bufferWithLineArgs;
	private int frameCounter;

	void Start() {
		Application.targetFrameRate = 300;

		m_cubeScale = new Vector3(scale, scale, scale);

		int particlesPerEdgeMinusTwo = particlesPerEdge-2;
		particlesPerBody = particlesPerEdge * particlesPerEdge * particlesPerEdge - particlesPerEdgeMinusTwo*particlesPerEdgeMinusTwo*particlesPerEdgeMinusTwo;
		particleDiameter = scale / particlesPerEdge;

		InitArrays();

		InitRigidBodies();

		InitParticles();

		InitBuffers();

		InitShader();

		voxelGridArray = new int[gridSize.x * gridSize.y * gridSize.z * 4];

		
		Debug.Log("nparticles: " + rigidBodyCount * particlesPerBody);
		// initialize constants

		InitInstancing();
		
	}

	void InitArrays()
    {
		//matricesArray = new Matrix4x4[rigidBodyCount];
		positionArray = new Vector3[rigidBodyCount];
		quaternionArray = new Quaternion[rigidBodyCount];
		rigidBodyVelocitiesArray = new Vector3[rigidBodyCount];
		//rigidBodyInertialTensorsArray = new float[rigidBodyCount * 9];

		rigidBodiesArray = new RigidBody[rigidBodyCount];
		particlesArray = new Particle[rigidBodyCount * particlesPerBody];
	}

	void InitRigidBodies()
    {
		Vector3 spawnPosition = new Vector3(0, 10, 0);

		int pIndex = 0;

		for(int i=0; i<rigidBodyCount; i++)
        {
			Vector3 pos = spawnPosition + Random.insideUnitSphere * 10.0f;
			positionArray[i] = pos;
			quaternionArray[i] = Quaternion.identity;
			rigidBodyVelocitiesArray[i] = Vector3.zero;
			rigidBodiesArray[i] = new RigidBody(pos, pIndex, particlesPerBody);
			pIndex += particlesPerBody;
		}
	}

	void InitParticles()
    {
		int count = rigidBodyCount * particlesPerBody;

		particleVelocities = new Vector3[count];
		particlePositions = new Vector3[count];
		particleRelativePositions = new Vector3[count];
		particleForcesArray = new Vector3[count];

		// initialize buffers
		// initial relative positions
		// super dependent on 8/rigid body
		particleInitialRelativePositions = new Vector3[count];

		Vector3[] particleInitialsSmall = new Vector3[particlesPerBody];
		int initialRelativePositionIterator = 0;
		float centerer = scale * -0.5f + particleDiameter * 0.5f;
		Vector3 centeringOffset = new Vector3(centerer, centerer, centerer);

		for (int xIter = 0; xIter < particlesPerEdge; xIter++)
		{
			for (int yIter = 0; yIter < particlesPerEdge; yIter++)
			{
				for (int zIter = 0; zIter < particlesPerEdge; zIter++)
				{
					if (xIter == 0 || xIter == (particlesPerEdge - 1) || yIter == 0 || yIter == (particlesPerEdge - 1) || zIter == 0 || zIter == (particlesPerEdge - 1))
					{
						particleInitialsSmall[initialRelativePositionIterator] = centeringOffset + new Vector3(xIter * particleDiameter, yIter * particleDiameter, zIter * particleDiameter);
						initialRelativePositionIterator++;
					}
				}
			}
		}
		for (int i = 0; i < particleInitialRelativePositions.Length; i++)
		{
			particleInitialRelativePositions[i] = particleInitialsSmall[i % particlesPerBody];
		}

		//debugParticleIds = new int[debug_particle_id_count];
		int numOfParticles = rigidBodyCount * particlesPerBody;
		particleVoxelPositionsArray = new int[numOfParticles * 3];
	}

	void InitBuffers()
    {
		int total = rigidBodyCount;
		// Create initial buffers
		m_rigidBodyPositions = new ComputeBuffer(total, 3 * sizeof(float));
		m_previousRigidBodyPositions = new ComputeBuffer(total, 3 * sizeof(float));
		m_rigidBodyQuaternions = new ComputeBuffer(total, 4 * sizeof(float));
		m_previousRigidBodyQuaternions = new ComputeBuffer(total, 4 * sizeof(float));
		m_rigidBodyAngularVelocities = new ComputeBuffer(total, 3 * sizeof(float));
		m_rigidBodyVelocities = new ComputeBuffer(total, 3 * sizeof(float));
		//m_rigidBodyInertialTensors = new ComputeBuffer(total, 9 * sizeof(float));

		int n_particles = particlePositions.Length;
		m_particleInitialRelativePositions = new ComputeBuffer(n_particles, 3 * sizeof(float));
		m_particlePositions = new ComputeBuffer(n_particles, 3 * sizeof(float));
		m_particleRelativePositions = new ComputeBuffer(n_particles, 3 * sizeof(float));
		m_particleVelocities = new ComputeBuffer(n_particles, 3 * sizeof(float));
		m_particleForces = new ComputeBuffer(n_particles, 3 * sizeof(float));

		int numGridCells = gridSize.x * gridSize.y * gridSize.z;
		m_voxelCollisionGrid = new ComputeBuffer(numGridCells, 4 * sizeof(int));
		//m_debugParticleVoxelPositions = new ComputeBuffer(n_particles, 3 * sizeof(int));
		//m_debugParticleIds = new ComputeBuffer(debug_particle_id_count, sizeof(int));

		m_particleInitialRelativePositions.SetData(particleInitialRelativePositions);

		m_rigidBodyPositions.SetData(positionArray);
		m_rigidBodyQuaternions.SetData(quaternionArray);
		m_rigidBodyVelocities.SetData(rigidBodyVelocitiesArray);
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
		shader.SetFloat("particleMass", m_cubeMass / particlesPerBody);
		shader.SetFloats("gridStartPosition", new float[] { gridStartPosition.x, gridStartPosition.y, gridStartPosition.z });

		int particleCount = rigidBodyCount * particlesPerBody;
		// Get Kernels
		kernelGenerateParticleValues = shader.FindKernel("GenerateParticleValues");
		kernelClearGrid = shader.FindKernel("ClearGrid");
		kernelPopulateGrid = shader.FindKernel("PopulateGrid");
		kernelCollisionDetection = shader.FindKernel("CollisionDetection");
		kernelComputeMomenta = shader.FindKernel("ComputeMomenta");
		kernelComputePositionAndRotation = shader.FindKernel("ComputePositionAndRotation");

		// Count Thread Groups
		m_threadGroupsPerRigidBody = Mathf.CeilToInt(rigidBodyCount / 8.0f);
		m_threadGroupsPerParticle = Mathf.CeilToInt(particleCount / 8f);
		m_threadGroupsPerGridCell = Mathf.CeilToInt((gridSize.x * gridSize.y * gridSize.z) / 8f);

		// Bind buffers

		// kernel 0 GenerateParticleValues
		shader.SetBuffer(kernelGenerateParticleValues, "rigidBodyPositions", m_rigidBodyPositions);
		shader.SetBuffer(kernelGenerateParticleValues, "rigidBodyQuaternions", m_rigidBodyQuaternions);
		shader.SetBuffer(kernelGenerateParticleValues, "rigidBodyAngularVelocities", m_rigidBodyAngularVelocities);
		shader.SetBuffer(kernelGenerateParticleValues, "rigidBodyVelocities", m_rigidBodyVelocities);
		shader.SetBuffer(kernelGenerateParticleValues, "particleInitialRelativePositions", m_particleInitialRelativePositions);
		shader.SetBuffer(kernelGenerateParticleValues, "particlePositions", m_particlePositions);
		shader.SetBuffer(kernelGenerateParticleValues, "particleRelativePositions", m_particleRelativePositions);
		shader.SetBuffer(kernelGenerateParticleValues, "particleVelocities", m_particleVelocities);
		
		// kernel 1 ClearGrid
		shader.SetBuffer(kernelClearGrid, "voxelCollisionGrid", m_voxelCollisionGrid);

		// kernel 2 Populate Grid
		//shader.SetBuffer(kernelPopulateGrid, "debugParticleVoxelPositions", m_debugParticleVoxelPositions);
		shader.SetBuffer(kernelPopulateGrid, "voxelCollisionGrid", m_voxelCollisionGrid);
		shader.SetBuffer(kernelPopulateGrid, "particlePositions", m_particlePositions);
		
		// kernel 3 Collision Detection
		shader.SetBuffer(kernelCollisionDetection, "particlePositions", m_particlePositions);
		shader.SetBuffer(kernelCollisionDetection, "particleVelocities", m_particleVelocities);
		shader.SetBuffer(kernelCollisionDetection, "voxelCollisionGrid", m_voxelCollisionGrid);
		shader.SetBuffer(kernelCollisionDetection, "particleForces", m_particleForces);

		// kernel 4 Computation of Momenta
		shader.SetBuffer(kernelComputeMomenta, "particleForces", m_particleForces);
		shader.SetBuffer(kernelComputeMomenta, "particleRelativePositions", m_particleRelativePositions);
		shader.SetBuffer(kernelComputeMomenta, "rigidBodyAngularVelocities", m_rigidBodyAngularVelocities);
		shader.SetBuffer(kernelComputeMomenta, "rigidBodyVelocities", m_rigidBodyVelocities);
		//shader.SetBuffer(kernelComputeMomenta, "debugParticleIds", m_debugParticleIds);
		shader.SetBuffer(kernelComputeMomenta, "rigidBodyQuaternions", m_rigidBodyQuaternions);

		// kernel 5 Compute Position and Rotation
		shader.SetBuffer(kernelComputePositionAndRotation, "rigidBodyVelocities", m_rigidBodyVelocities);
		shader.SetBuffer(kernelComputePositionAndRotation, "rigidBodyAngularVelocities", m_rigidBodyAngularVelocities);
		shader.SetBuffer(kernelComputePositionAndRotation, "rigidBodyPositions", m_rigidBodyPositions);
		shader.SetBuffer(kernelComputePositionAndRotation, "rigidBodyQuaternions", m_rigidBodyQuaternions);
	}

	void InitInstancing() { 
		// Setup Indirect Renderer
		cubeMaterial.SetBuffer("positions", m_rigidBodyPositions);
		cubeMaterial.SetBuffer("quaternions", m_rigidBodyQuaternions);

		if (m_debugWireframe)
		{
			int numOfParticles = rigidBodyCount * particlesPerBody;

			uint[] sphereArgs = new uint[] { sphereMesh.GetIndexCount(0), (uint)numOfParticles, 0, 0, 0 };
			m_bufferWithSphereArgs = new ComputeBuffer(1, sphereArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			m_bufferWithSphereArgs.SetData(sphereArgs);

			uint[] lineArgs = new uint[] { lineMesh.GetIndexCount(0), (uint)numOfParticles, 0, 0, 0 };
			m_bufferWithLineArgs = new ComputeBuffer(1, lineArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			m_bufferWithLineArgs.SetData(lineArgs);

			sphereMaterial.SetBuffer("positions", m_particlePositions);
			sphereMaterial.SetVector("scale", new Vector4(particleDiameter * 0.5f, particleDiameter * 0.5f, particleDiameter * 0.5f, 1.0f));
			lineMaterial.SetBuffer("positions", m_particlePositions);
			lineMaterial.SetBuffer("vectors", m_particleVelocities);
		}
	}

	/*void InitInertialTensor() {
		// Inertial tensor of a cube formula taken from textbook:
		// "Essential Mathematics for Games and Interactive Applications"
		// by James Van Verth and Lars Bishop
		float twoDimSq = 2.0f * (scale * scale);
		float inertialTensorFactor = m_cubeMass * 1.0f / 12.0f * twoDimSq;
		float[] inertialTensor = {
			inertialTensorFactor, 0.0f, 0.0f,
			0.0f, inertialTensorFactor, 0.0f,
			0.0f, 0.0f, inertialTensorFactor
		};
		float[] inverseInertialTensor;
		GPUPhysics.Invert(ref inertialTensor, out inverseInertialTensor);
		float[] quickInverseInertialTensor = {
			1.0f/inertialTensorFactor, 0.0f, 0.0f,
			0.0f, 1.0f/inertialTensorFactor, 0.0f,
			0.0f, 0.0f, 1.0f/inertialTensorFactor
		};
		shader.SetFloats("inertialTensor", inertialTensor);
		shader.SetFloats("inverseInertialTensor", quickInverseInertialTensor);
	}*/

	void WireframeRender()
    {
		shader.SetFloat("particleDiameter", scale / particlesPerEdge);
		shader.SetFloat("springCoefficient", springCoefficient);
		shader.SetFloat("dampingCoefficient", dampingCoefficient);
		shader.SetFloat("frictionCoefficient", frictionCoefficient);
		shader.SetFloat("angularFrictionCoefficient", angularFrictionCoefficient);
		shader.SetFloat("gravityCoefficient", gravityCoefficient);
		shader.SetFloat("tangentialCoefficient", tangentialCoefficient);
		shader.SetFloat("angularForceScalar", angularForceScalar);
		shader.SetFloat("linearForceScalar", linearForceScalar);
		int particlesPerBody = 8;
		shader.SetFloat("particleMass", m_cubeMass / particlesPerBody);

		Graphics.DrawMeshInstancedIndirect(sphereMesh, 0, sphereMaterial, m_bounds, m_bufferWithSphereArgs);
		
		lineMaterial.SetBuffer("positions", m_rigidBodyPositions);
		lineMaterial.SetBuffer("vectors", m_rigidBodyAngularVelocities);

		Graphics.DrawMeshInstancedIndirect(lineMesh, 0, lineMaterial, m_bounds, m_bufferWithLineArgs);
		m_rigidBodyQuaternions.GetData(quaternionArray);

	}

	void Update() {
		
		if (m_bufferWithArgs == null || m_debugWireframe != m_lastDebugWireframe) {
			uint indexCountPerInstance = cubeMesh.GetIndexCount(0);
			uint instanceCount = (uint)rigidBodyCount;
			uint startIndexLocation = 0;
			uint baseVertexLocation = 0;
			uint startInstanceLocation = 0;
			uint[] args = new uint[] { indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation };
			m_bufferWithArgs = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			m_bufferWithArgs.SetData(args);
			m_lastDebugWireframe = m_debugWireframe;
		}
		if (frameCounter++ < 10) {
			return;
		}

		ticker += Time.deltaTime;
		float _dt = 1.0f / tick_rate;

		while (ticker >= _dt) {
			ticker -= _dt;

			shader.SetFloat(deltaTimeID, _dt);

			shader.Dispatch(kernelGenerateParticleValues, m_threadGroupsPerRigidBody, 1, 1);
			shader.Dispatch(kernelClearGrid, m_threadGroupsPerGridCell, 1, 1);
			shader.Dispatch(kernelPopulateGrid, m_threadGroupsPerParticle, 1, 1);
			shader.Dispatch(kernelCollisionDetection, m_threadGroupsPerParticle, 1, 1);
			shader.Dispatch(kernelComputeMomenta, m_threadGroupsPerRigidBody, 1, 1);
			shader.Dispatch(kernelComputePositionAndRotation, m_threadGroupsPerRigidBody, 1, 1);
		}

		if (m_debugWireframe) {
			WireframeRender();
        }
        else
        {
            Graphics.DrawMeshInstancedIndirect(cubeMesh, 0, cubeMaterial, m_bounds, m_bufferWithArgs);
		}
	}

	void OnDestroy() {
		m_rigidBodyPositions.Release();
		m_rigidBodyQuaternions.Release();
		m_rigidBodyAngularVelocities.Release();
		m_rigidBodyVelocities.Release();

		m_particleInitialRelativePositions.Release();
		m_particlePositions.Release();
		m_particleRelativePositions.Release();
		m_particleVelocities.Release();
		m_particleForces.Release();

		//m_debugParticleVoxelPositions.Release();
		m_voxelCollisionGrid.Release();

		m_previousRigidBodyQuaternions.Release();
		m_previousRigidBodyPositions.Release();

		//m_debugParticleIds.Release();

		if (m_bufferWithSphereArgs != null) {
			m_bufferWithSphereArgs.Release();
		}
		if (m_bufferWithLineArgs != null) {
			m_bufferWithLineArgs.Release();
		}
		if (m_bufferWithArgs != null) {
			m_bufferWithArgs.Release();
		}
	}

	/*public static int M(int row, int column) {
		return (row-1) * 3 + (column-1);
	}
	public static void Invert(ref float[] value, out float[] result) {
		float d11 = value[M(2,2)] * value[M(3,3)] + value[M(2,3)] * -value[M(3,2)];
		float d12 = value[M(2,1)] * value[M(3,3)] + value[M(2,3)] * -value[M(3,1)];
		float d13 = value[M(2,1)] * value[M(3,2)] + value[M(2,2)] * -value[M(3,1)];

		float det = value[M(1,1)] * d11 - value[M(1,2)] * d12 + value[M(1,3)] * d13;
		result = new float[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

		if (Mathf.Abs(det) == 0.0f) {
			return;
		}

		det = 1f / det;

		float d21 = value[M(1, 2)] * value[M(3, 3)] + value[M(1, 3)] * -value[M(3, 2)];
		float d22 = value[M(1, 1)] * value[M(3, 3)] + value[M(1, 3)] * -value[M(3, 1)];
		float d23 = value[M(1, 1)] * value[M(3, 2)] + value[M(1, 2)] * -value[M(3, 1)];

		float d31 = (value[M(1, 2)] * value[M(2, 3)]) - (value[M(1, 3)] * value[M(2, 2)]);
		float d32 = (value[M(1, 1)] * value[M(2, 3)]) - (value[M(1, 3)] * value[M(2, 1)]);
		float d33 = (value[M(1, 1)] * value[M(2, 2)]) - (value[M(1, 2)] * value[M(2, 1)]);

		result[M(1,1)] = +d11 * det; result[M(1,2)] = -d21 * det; result[M(1,3)] = +d31 * det;
		result[M(2,1)] = -d12 * det; result[M(2,2)] = +d22 * det; result[M(2,3)] = -d32 * det;
		result[M(3,1)] = +d13 * det; result[M(3,2)] = -d23 * det; result[M(3,3)] = +d33 * det;
	}*/
}