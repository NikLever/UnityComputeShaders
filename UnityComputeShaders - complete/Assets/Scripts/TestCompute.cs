using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCompute : MonoBehaviour
{
    public ComputeShader shader;

    int[] debugArray;
    ComputeBuffer debugBuffer;

    int groupSize = 16;
    int kernelCSMain;
    
    // Start is called before the first frame update
    void Start()
    {
        int count = groupSize * 8;
        debugArray = new int[count];
        debugBuffer = new ComputeBuffer(count, sizeof(int), ComputeBufferType.Structured);

        kernelCSMain = shader.FindKernel("CSMain");
        shader.SetBuffer(kernelCSMain, "debugBuffer", debugBuffer);
        shader.SetInt("debugCount", count);
    }

    // Update is called once per frame
    void Update()
    {
        shader.Dispatch(kernelCSMain, groupSize, 1, 1);
        
        debugBuffer.GetData(debugArray);

        for(int i=0; i<debugArray.Length; i++)
        {
            Debug.Log(i + ":" + debugArray[i]);
        }
    }

    void OnDestroy()
    {
        debugBuffer.Dispose();    
    }
}
