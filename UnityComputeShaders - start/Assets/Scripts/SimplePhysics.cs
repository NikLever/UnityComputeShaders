using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePhysics : MonoBehaviour
{
    public struct Ball
    {
        public Vector3 position;
        public Vector3 velocity;
        public Color color;

        public Ball(float posRange, float maxVel)
        {
            position.x = Random.value * posRange - posRange/2;
            position.y = Random.value * posRange;
            position.z = Random.value * posRange - posRange / 2;
            velocity.x = Random.value * maxVel - maxVel/2;
            velocity.y = Random.value * maxVel - maxVel / 2;
            velocity.z = Random.value * maxVel - maxVel / 2;
            color.r = Random.value;
            color.g = Random.value;
            color.b = Random.value;
            color.a = 1;
        }
    }

    public ComputeShader shader;

    public Mesh ballMesh;
    public Material ballMaterial;
    public int ballsCount;
    public float radius = 0.08f;
    
    int kernelHandle;
    ComputeBuffer ballsBuffer;
    ComputeBuffer argsBuffer;
    uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    Ball[] ballsArray;
    int groupSizeX;
    int numOfBalls;
    Bounds bounds;
   
    MaterialPropertyBlock props;

    void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");

        uint x;
        shader.GetKernelThreadGroupSizes(kernelHandle, out x, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)ballsCount / (float)x);
        numOfBalls = groupSizeX * (int)x;

        props = new MaterialPropertyBlock();
        props.SetFloat("_UniqueID", Random.value);

        bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

        InitBalls();
        InitShader();
    }

    private void InitBalls()
    {
        ballsArray = new Ball[numOfBalls];

        for (int i = 0; i < numOfBalls; i++)
        {
            ballsArray[i] = new Ball(4, 0.01f);
        }
    }

    void InitShader()
    {
        ballsBuffer = new ComputeBuffer(numOfBalls, 10 * sizeof(float));
        ballsBuffer.SetData(ballsArray);

        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        if (ballMesh != null)
        {
            args[0] = (uint)ballMesh.GetIndexCount(0);
            args[1] = (uint)numOfBalls;
            args[2] = (uint)ballMesh.GetIndexStart(0);
            args[3] = (uint)ballMesh.GetBaseVertex(0);
        }
        argsBuffer.SetData(args);

        shader.SetBuffer(this.kernelHandle, "ballsBuffer", ballsBuffer);
        shader.SetInt("ballsCount", numOfBalls);
        shader.SetVector("limitsXZ", new Vector4(-2.5f+radius, 2.5f-radius, -2.5f+radius, 2.5f-radius));
        shader.SetFloat("floorY", -2.5f+radius);
        shader.SetFloat("radius", radius);

        ballMaterial.SetFloat("_Radius", radius*2);
        ballMaterial.SetBuffer("ballsBuffer", ballsBuffer);
    }

    void Update()
    {
        int iterations = 5;
        shader.SetFloat("deltaTime", Time.deltaTime/iterations);

        for (int i = 0; i < iterations; i++)
        {
            shader.Dispatch(this.kernelHandle, groupSizeX, 1, 1);
        }

        Graphics.DrawMeshInstancedIndirect(ballMesh, 0, ballMaterial, bounds, argsBuffer, 0, props);
    }

    void OnDestroy()
    {
        if (ballsBuffer != null)
        {
            ballsBuffer.Dispose();
        }

        if (argsBuffer != null)
        {
            argsBuffer.Dispose();
        }
    }
}

