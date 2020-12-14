using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GPUPhysicsCompute : MonoBehaviour
{
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
	public Mesh cubeMesh
	{
		get
		{
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
	public int rigidBodyCount = 1000;
	[Range(1, 20)]
	public int stepsPerUpdate = 10;

	// calculated
	int particlesPerBody;
	float particleDiameter;

	RigidBody[] rigidBodiesArray;
	Particle[] particlesArray;
	uint[] argsArray = new uint[] { 0, 0, 0, 0, 0 };

	ComputeBuffer rigidBodiesBuffer;
	ComputeBuffer particlesBuffer;
	private ComputeBuffer argsBuffer;

	private int kernelGenerateParticleValues;
	private int kernelCollisionDetection;
	private int kernelComputeMomenta;
	private int kernelComputePositionAndRotation;

	private int groupsPerRigidBody;
	private int groupsPerParticle;
	private int deltaTimeID;

	int activeCount = 0;

	private int frameCounter;

	void Start()
	{
		InitArrays();

		InitRigidBodies();

		InitParticles();

		InitBuffers();

		InitShader();

		InitInstancing();

	}

	void InitArrays()
	{
		particlesPerBody = particlesPerEdge * particlesPerEdge * particlesPerEdge;

		rigidBodiesArray = new RigidBody[rigidBodyCount];
		particlesArray = new Particle[rigidBodyCount * particlesPerBody];
	}

	void InitRigidBodies()
	{
		int pIndex = 0;

		for (int i = 0; i < rigidBodyCount; i++)
		{
			Vector3 pos = Random.insideUnitSphere * 5.0f;
			pos.y += 15;
			rigidBodiesArray[i] = new RigidBody(pos, pIndex, particlesPerBody);
			pIndex += particlesPerBody;
		}
	}

	void InitParticles()
	{
		particleDiameter = scale / particlesPerEdge;

		// initial local particle positions within a rigidbody
		int index = 0;
		float centerer = (particleDiameter - scale) * 0.5f;
		Vector3 centeringOffset = new Vector3(centerer, centerer, centerer);

		for (int x = 0; x < particlesPerEdge; x++)
		{
			for (int y = 0; y < particlesPerEdge; y++)
			{
				for (int z = 0; z < particlesPerEdge; z++)
				{
					Vector3 pos = centeringOffset + new Vector3(x, y, z) * particleDiameter;
					for (int i = 0; i < rigidBodyCount; i++)
					{
						RigidBody body = rigidBodiesArray[i];
						particlesArray[body.particleIndex + index] = new Particle(pos);
					}
					index++;
				}
			}
		}
		Debug.Log("particleCount: " + rigidBodyCount * particlesPerBody);
	}

	void InitBuffers()
	{
		rigidBodiesBuffer = new ComputeBuffer(rigidBodyCount, SIZE_RIGIDBODY);
		rigidBodiesBuffer.SetData(rigidBodiesArray);

		int numOfParticles = rigidBodyCount * particlesPerBody;
		particlesBuffer = new ComputeBuffer(numOfParticles, SIZE_PARTICLE);
		particlesBuffer.SetData(particlesArray);
	}

	void InitShader()
	{
		deltaTimeID = Shader.PropertyToID("deltaTime");

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
		int particleCount = rigidBodyCount * particlesPerBody;
		shader.SetInt("particleCount", particleCount);

		// Get Kernels
		kernelGenerateParticleValues = shader.FindKernel("GenerateParticleValues");
		kernelCollisionDetection = shader.FindKernel("CollisionDetection");
		kernelComputeMomenta = shader.FindKernel("ComputeMomenta");
		kernelComputePositionAndRotation = shader.FindKernel("ComputePositionAndRotation");

		// Count Thread Groups
		groupsPerRigidBody = Mathf.CeilToInt(rigidBodyCount / 8.0f);
		groupsPerParticle = Mathf.CeilToInt(particleCount / 8f);

		// Bind buffers

		// kernel 0 GenerateParticleValues
		shader.SetBuffer(kernelGenerateParticleValues, "rigidBodiesBuffer", rigidBodiesBuffer);
		shader.SetBuffer(kernelGenerateParticleValues, "particlesBuffer", particlesBuffer);

		// kernel 1 Collision Detection
		shader.SetBuffer(kernelCollisionDetection, "particlesBuffer", particlesBuffer);

		// kernel 2 Computation of Momenta
		shader.SetBuffer(kernelComputeMomenta, "rigidBodiesBuffer", rigidBodiesBuffer);
		shader.SetBuffer(kernelComputeMomenta, "particlesBuffer", particlesBuffer);

		// kernel 3 Compute Position and Rotation
		shader.SetBuffer(kernelComputePositionAndRotation, "rigidBodiesBuffer", rigidBodiesBuffer);
	}

	void InitInstancing()
	{
		// Setup Indirect Renderer
		cubeMaterial.SetBuffer("rigidBodiesBuffer", rigidBodiesBuffer);

		argsArray[0] = cubeMesh.GetIndexCount(0);
		argsBuffer = new ComputeBuffer(1, argsArray.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
		argsBuffer.SetData(argsArray);
	}

	void Update()
	{
		if (activeCount < rigidBodyCount && frameCounter++ > 5)
		{
			activeCount++;
			frameCounter = 0;
			shader.SetInt("activeCount", activeCount);
			argsArray[1] = (uint)activeCount;
			argsBuffer.SetData(argsArray);
		}

		float dt = Time.deltaTime / stepsPerUpdate;
		shader.SetFloat(deltaTimeID, dt);

		for (int i = 0; i < stepsPerUpdate; i++)
		{
			shader.Dispatch(kernelGenerateParticleValues, groupsPerRigidBody, 1, 1);
			shader.Dispatch(kernelCollisionDetection, groupsPerParticle, 1, 1);
			shader.Dispatch(kernelComputeMomenta, groupsPerRigidBody, 1, 1);
			shader.Dispatch(kernelComputePositionAndRotation, groupsPerRigidBody, 1, 1);
		}

		Graphics.DrawMeshInstancedIndirect(cubeMesh, 0, cubeMaterial, bounds, argsBuffer);
	}

	void OnDestroy()
	{
		rigidBodiesBuffer.Release();
		particlesBuffer.Release();

		if (argsBuffer != null)
		{
			argsBuffer.Release();
		}
	}
}