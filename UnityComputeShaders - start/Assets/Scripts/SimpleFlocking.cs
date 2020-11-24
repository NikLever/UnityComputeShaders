using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleFlocking : MonoBehaviour
{
    public struct Boid
    {
        public Vector3 position;
        public Vector3 direction;
        public float noise_offset;

        public Boid(Vector3 pos, float offset)
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            direction.x = 0;
            direction.y = 0;
            direction.z = 0;
            noise_offset = offset;
        }
    }

    public ComputeShader shader;

    public float rotationSpeed = 1f;
    public float boidSpeed = 1f;
    public float neighbourDistance = 1f;
    public float boidSpeedVariation = 1f;
    public GameObject boidPrefab;
    public int boidsCount;
    public float spawnRadius;
    public Transform target;

    int kernelHandle;
    ComputeBuffer boidsBuffer;
    Boid[] boidsArray;
    GameObject[] boids;
    int groupSizeX;
    int numOfBoids;

    void Start()
    {
        kernelHandle = shader.FindKernel("CSMain");

        uint x;
        shader.GetKernelThreadGroupSizes(kernelHandle, out x, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)boidsCount / (float)x);
        numOfBoids = groupSizeX * (int)x;

        InitBoids();
        InitShader();
    }

    private void InitBoids()
    {
        boids = new GameObject[numOfBoids];
        boidsArray = new Boid[numOfBoids];

        for (int i = 0; i < numOfBoids; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            float offset = Random.value * 1000.0f;
            boidsArray[i] = new Boid(pos, offset);
            boids[i] = Instantiate(boidPrefab, pos, Quaternion.identity) as GameObject;
            boidsArray[i].direction = boids[i].transform.forward;
        }
    }

    void InitShader()
    {
        boidsBuffer = new ComputeBuffer(numOfBoids, 7 * sizeof(float));
        boidsBuffer.SetData(boidsArray);

        shader.SetBuffer(this.kernelHandle, "boidsBuffer", boidsBuffer);
        shader.SetFloat("rotationSpeed", rotationSpeed);
        shader.SetFloat("boidSpeed", boidSpeed);
        shader.SetFloat("boidSpeedVariation", boidSpeedVariation);
        shader.SetVector("flockPosition", target.transform.position);
        shader.SetFloat("neighbourDistance", neighbourDistance);
        shader.SetInt("boidsCount", boidsCount);
    }

    void Update()
    {
        shader.SetFloat("time", Time.time);
        shader.SetFloat("deltaTime", Time.deltaTime);

        shader.Dispatch(this.kernelHandle, groupSizeX, 1, 1);

        boidsBuffer.GetData(boidsArray);

        for (int i = 0; i < boidsArray.Length; i++)
        {
            boids[i].transform.localPosition = boidsArray[i].position;

            if (!boidsArray[i].direction.Equals(Vector3.zero))
            {
                boids[i].transform.rotation = Quaternion.LookRotation(boidsArray[i].direction);
            }

        }
    }

    void OnDestroy()
    {
        if (boidsBuffer!=null)
        {
            boidsBuffer.Dispose();
        }
    }
}

