using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxelization : MonoBehaviour
{
    struct Particle
    {
        public Vector3 position;
    };

    public float particleSize;
    public GameObject prefab;

    List<Particle> particles = new List<Particle>();

    // Start is called before the first frame update
    void Start()
    {
        if (prefab)
        {
            Voxelize(prefab);
        }       
    }

    void Voxelize(GameObject go)
    {
        particles.Clear();
        foreach (Transform child in go.transform)
        {
            GameObject.DestroyImmediate(child.gameObject);
        }
        
        MeshFilter mf = go.GetComponent<MeshFilter>();
        Mesh mesh = mf.sharedMesh;

        Vector3 minExtents = mesh.bounds.center - mesh.bounds.extents;
        Vector3 maxExtents = mesh.bounds.center + mesh.bounds.extents;

        RaycastHit hit;

        float radius = particleSize * 0.5f;
        Vector3 rayOffset = minExtents;
        rayOffset.x += radius;
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
                        int n = Mathf.RoundToInt(frontPt.z / particleSize);
                        frontPt.z = n * particleSize;
                        while (frontPt.z < (backPt.z-radius))
                        {
                            //Add a new Particle
                            Particle particle = new Particle();
                            particle.position = frontPt;
                            particle.position.z += radius;
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
