using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class BoxBlur : BlurCore
{
    protected override void Init()
    {
        base.Init();
        base.CheckForKernels();
        init = true;
    }

    private void OnValidate()
    {
        if(!init)
            Init();
           
        SetRadius();
        
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    { 
        CheckForErrorsAndResolution();
        DispatchWithSource(ref source, ref destination);
    }
}
