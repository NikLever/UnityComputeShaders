using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSlider : MonoBehaviour
{
    float offset;

    // Start is called before the first frame update
    void Start()
    {
        offset = Random.value * Mathf.PI * 2;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Sin(Time.time + offset);
        transform.position = pos;
    }
}
