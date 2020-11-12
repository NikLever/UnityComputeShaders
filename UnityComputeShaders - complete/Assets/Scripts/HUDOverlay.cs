using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HUDOverlay : BasePP
{
   protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
   {
        if (shader) shader.SetFloat("time", Time.time);
        base.OnRenderImage(source, destination);
   }

}
