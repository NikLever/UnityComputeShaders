using UnityEngine;
using System.Collections;

public class MeshDeform : MonoBehaviour
{
    public ComputeShader shader;
	public float radius;
	
    int kernelHandle;
    
    // Use this for initialization
    void Start()
    {
    
        if (InitData())
        {
            InitShader();
        }
    }

    private bool InitData()
    {
        kernelHandle = shader.FindKernel("CSMain");

        return true;
    }

    private void InitShader()
    {
        shader.SetFloat("radius", radius);

    }

    void GetVerticesFromGPU()
    {
        
    }

    void Update(){
        if (shader)
        {
            float delta = (Mathf.Sin(Time.time) + 1)/ 2;
            shader.SetFloat("delta", delta);
            shader.Dispatch(kernelHandle, 1, 1, 1);
            
            GetVerticesFromGPU();
        }
    }

    void OnDestroy()
    {
        
    }
}

