using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitingStars : MonoBehaviour
{
    public int starCount = 17;
    public ComputeShader shader;

    public GameObject prefab;

    ComputeBuffer resultBuffer;
    int kernelHandle;
    uint threadGroupSizeX;
    int groupSizeX;
    Vector3[] output;

    Transform[] stars;

    void Start()
    {
        //program we're executing
        kernelHandle = shader.FindKernel("OrbitingStars");
        shader.GetKernelThreadGroupSizes(kernelHandle, out threadGroupSizeX, out _, out _);
        groupSizeX = (int)((starCount + threadGroupSizeX - 1) / threadGroupSizeX);

        //buffer on the gpu in the ram
        resultBuffer = new ComputeBuffer(starCount, sizeof(float) * 3);
        shader.SetBuffer(kernelHandle, "Result", resultBuffer);
        output = new Vector3[starCount];

        //star we use for visualisation
        stars = new Transform[starCount];
        for (int i = 0; i < starCount; i++)
        {
            stars[i] = Instantiate(prefab, transform).transform;
        }
    }

    void Update()
    {
        shader.SetFloat("time", Time.time);
        shader.Dispatch(kernelHandle, groupSizeX, 1, 1);
        resultBuffer.GetData(output);

        for (int i = 0; i < stars.Length; i++)
            stars[i].localPosition = output[i];
    }

    void OnDestroy()
    {
        resultBuffer.Dispose();
    }
}
