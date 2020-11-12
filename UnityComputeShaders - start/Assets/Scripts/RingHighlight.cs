using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RingHighlight : BasePP
{
    [Range(0.0f, 100.0f)]
    public float radius = 10;
    [Range(0.0f, 100.0f)]
    public float softenEdge;
    [Range(0.0f, 1.0f)]
    public float shade;
    public Transform trackedObject;

    protected override void Init()
    {
        kernelName = "Highlight";
        base.Init();
    }

    private void OnValidate()
    {
        if(!init)
            Init();
           
        SetProperties();
    }

    protected void SetProperties()
    {
        float rad = (radius / 100.0f) * texSize.y;
        shader.SetFloat("radius", rad);
        shader.SetFloat("edgeWidth", rad * softenEdge / 100.0f);
        shader.SetFloat("shade", shade);
    }

    protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!init || shader == null)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            CheckResolution(out _);
            DispatchWithSource(ref source, ref destination);
        }
    }

}
