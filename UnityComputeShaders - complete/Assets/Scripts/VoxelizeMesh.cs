using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelizeMesh : MonoBehaviour 
{
    public Mesh meshToVoxelize;
    public int yParticleCount = 4;
    public int layer = 9;

    float particleSize;

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
        GameObject go = new GameObject();
        go.layer = 9;
        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        MeshCollider collider = go.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;

        Vector3 minExtents = mesh.bounds.center - mesh.bounds.extents;
        Vector3 maxExtents = mesh.bounds.center + mesh.bounds.extents;

        RaycastHit hit;

        float radius = mesh.bounds.extents.y/yParticleCount;
        particleSize = radius * 2;
        Vector3 rayOffset = minExtents;
        Vector3 counts = mesh.bounds.extents / radius;
        Vector3Int particleCounts = new Vector3Int((int)counts.x, (int)counts.y, (int)counts.z);

        //Debug.Log("minExtents before " + minExtents);
        if ((particleCounts.x % 2) == 0)
        {
            minExtents.x += (mesh.bounds.extents.x - (float)particleCounts.x * radius);
        }
        //Debug.Log("minExtents after " + minExtents);
        float offsetZ = 0;
        if ((particleCounts.z % 2) == 0)
        {
            offsetZ += (mesh.bounds.extents.z - (float)particleCounts.z * radius);
        }
        //Debug.Log("offsetZ " + offsetZ);

        rayOffset.y += radius;
        Vector3 scale = Vector3.one * particleSize;
        int layerMask = 1 << layer;//Voxelize mesh is in layer 9 by default

        while(rayOffset.y < maxExtents.y)
        {
            rayOffset.x = minExtents.x;

            while(rayOffset.x < maxExtents.x)
            {
                Vector3 rayOrigin = go.transform.position + rayOffset;

                if (Physics.Raycast(rayOrigin, Vector3.forward, out hit, 100.0f, layerMask))
                {
                    Vector3 frontPt = hit.point;
                    rayOrigin.z += maxExtents.z * 2;
                    if (Physics.Raycast(rayOrigin, Vector3.back, out hit, 100.0f, layerMask))
                    {
                        Vector3 backPt = hit.point;
                        int n = Mathf.CeilToInt(frontPt.z / particleSize);
                        frontPt.z = n * particleSize;
                        while (frontPt.z < backPt.z)
                        {
                            float gap = backPt.z - frontPt.z;
                            if (gap < radius * 0.5f) break;
                            positions.Add(frontPt);
                            frontPt.z += particleSize;
                        }
                    }
                }

                rayOffset.x += particleSize;
            }

            rayOffset.y += particleSize;
        }

    }
}
