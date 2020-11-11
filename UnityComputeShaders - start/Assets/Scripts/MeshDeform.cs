using UnityEngine;
using System.Collections;


public class MeshDeform : MonoBehaviour
{
    public ComputeShader shader;
    [Range(0.5f, 2.0f)]
	public float radius;
	
    int kernelHandle;
    Mesh mesh;
    
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

        MeshFilter mf = GetComponent<MeshFilter>();

        if (mf == null)
        {
            Debug.Log("No MeshFilter found");
            return false;
        }

        InitVertexArrays(mf.mesh);
        InitGPUBuffers();

        mesh = mf.mesh;

        return true;
    }

    private void InitShader()
    {
        shader.SetFloat("radius", radius);

    }
    
    private void InitVertexArrays(Mesh mesh)
    {
        
    }

    private void InitGPUBuffers()
    {
        
    }
    
    void GetVerticesFromGPU()
    {
        
    }

    void Update(){
        if (shader)
        {
        	shader.SetFloat("radius", radius);
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

