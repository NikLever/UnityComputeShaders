using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxelization : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
        VoxelizeMesh voxelizeMesh = GetComponent<VoxelizeMesh>();
        voxelizeMesh.Voxelize(voxelizeMesh.meshToVoxelize);
        float pS = voxelizeMesh.ParticleSize;
        Vector3 scale = new Vector3(pS, pS, pS);

        //Create balls
    }
}
