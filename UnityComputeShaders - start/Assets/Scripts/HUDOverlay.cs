using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HUDOverlay : BaseCompletePP
{
    public Color axisColor = new Color(0.8f, 0.8f, 0.8f, 1);
    public Color sweepColor = new Color(0.1f, 0.3f, 0.1f, 1);

    private void OnValidate()
    {
        if (!init)
            Init();

        SetProperties();
    }

    protected void SetProperties()
    {
        shader.SetVector("axisColor", axisColor);
        shader.SetVector("sweepColor", sweepColor);
    }

    protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (shader) shader.SetFloat("time", Time.time);
        base.OnRenderImage(source, destination);
    }

}
