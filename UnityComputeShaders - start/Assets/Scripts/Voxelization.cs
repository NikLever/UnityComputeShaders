using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxelization : MonoBehaviour
{
    struct Particle
    {
        public Vector3 position;
    };

    public Mesh meshToVoxelize;
    public int yParticleCount = 4;

    public GameObject prefab;

    List<Particle> particles = new List<Particle>();

    // Start is called before the first frame update
    void Start()
    {
        if (meshToVoxelize)
        {
            Voxelize(meshToVoxelize);
        }       
    }

    void Voxelize(Mesh mesh)
    {
        particles.Clear();
        foreach (Transform child in prefab.transform)
        {
            GameObject.DestroyImmediate(child.gameObject);
        }

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
        float particleSize = radius * 2;
        Vector3 rayOffset = minExtents;
        Vector3 counts = mesh.bounds.extents / radius;
        Vector3Int particleCounts = new Vector3Int((int)counts.x, (int)counts.y, (int)counts.z);

        Debug.Log("minExtents before " + minExtents);
        if ((particleCounts.x % 2) == 0)
        {
            minExtents.x += (mesh.bounds.extents.x - (float)particleCounts.x * radius);
        }
        Debug.Log("minExtents after " + minExtents);
        float offsetZ = 0;
        if ((particleCounts.z % 2) == 0)
        {
            offsetZ += (mesh.bounds.extents.z - (float)particleCounts.z * radius);
        }
        Debug.Log("offsetZ " + offsetZ);

        rayOffset.y += radius;
        Vector3 scale = Vector3.one * particleSize;
        int layerMask = 1 << 9;//Voxelize mesh is in layer 9

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
                            //Add a new Particle
                            Particle particle = new Particle();
                            particle.position = frontPt;
                            //particle.position.z += offsetZ;
                            particles.Add(particle);
                            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            ball.transform.position = particle.position;
                            ball.transform.localScale = scale;
                            ball.transform.parent = go.transform;
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
