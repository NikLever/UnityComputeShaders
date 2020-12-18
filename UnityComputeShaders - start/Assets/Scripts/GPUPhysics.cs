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
	public int rigidBodyCount = 1000;
	[Range(1, 20)]
	public int stepsPerUpdate = 10;
	
	// calculated
	private Vector3 cubeScale;
	
	int particlesPerBody;
	float particleDiameter;

	RigidBody[] rigidBodiesArray;
	Particle[] particlesArray;
	uint[] argsArray;
	
	ComputeBuffer rigidBodiesBuffer;
	ComputeBuffer particlesBuffer;
	private ComputeBuffer argsBuffer;
	private ComputeBuffer voxelGridBuffer;               
	
	private int kernelGenerateParticleValues;
	private int kernelCollisionDetection;
	private int kernelComputeMomenta;
	private int kernelComputePositionAndRotation;
	
	private int groupsPerRigidBody;
	private int groupsPerParticle;

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
			
	}

	void InitInstancing() {
		
	}

	void Update() {
        Graphics.DrawMeshInstancedIndirect(cubeMesh, 0, cubeMaterial, bounds, argsBuffer);
	}

	void OnDestroy() {
		rigidBodiesBuffer.Release();
		particlesBuffer.Release();

		if (argsBuffer != null) {
			argsBuffer.Release();
		}
	}
}