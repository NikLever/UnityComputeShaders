Shader "Custom/Particle" {
	Properties     
    {         
        _PointSize("Point size", Float) = 5.0     
    }  

	SubShader {
		Pass {
		Tags{ "RenderType" = "Opaque" }
		LOD 200
		Blend SrcAlpha one

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma vertex vert
		#pragma fragment frag

		uniform float _PointSize;

		#include "UnityCG.cginc"

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 5.0
		
		struct PS_INPUT{
			float4 position : SV_POSITION;
			float4 color : COLOR;
			float life : LIFE;
			float size: PSIZE;
		};
		

		PS_INPUT vert(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
		{
			PS_INPUT o = (PS_INPUT)0;

			// Color
			o.color = float4(1,0,0,0);

			// Position
			o.position = UnityObjectToClipPos(float4(0,0,0,0));
			o.size = 1;

			return o;
		}

		float4 frag(PS_INPUT i) : COLOR
		{
			return i.color;
		}


		ENDCG
		}
	}
	FallBack Off
}
