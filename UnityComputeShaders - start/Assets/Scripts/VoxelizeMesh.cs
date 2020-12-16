using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelizeMesh : MonoBehaviour 
{
    public Mesh meshToVoxelize;
    public int yParticleCount = 4;
    public int layer = 9;

    float particleSize = 0;

    public float ParticleSize{
        get{
            return particleSize; 
        }
    }

    List<Vector3> positions = new List<Vector3>();

    public List<Vector3> PositionList
    {
        get
        {
            return positions;
        }
    }

    public void Voxelize(Mesh mesh)
    {
        
    }
}
