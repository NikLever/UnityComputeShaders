Shader "Custom/QuadParticle" {
	Properties     
    {
        _MainTex("Texture", 2D) = "white" {}     
    }  

	SubShader {
		Pass {
		Tags{ "RenderType"="Opaque"  }
		LOD 200
		
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag

		#include "UnityCG.cginc"

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 5.0
		
		struct v2f{
			float4 position : SV_POSITION;
			float4 color : COLOR;
			float2 uv: TEXCOORD0;
		};

		sampler2D _MainTex;
		
		v2f vert(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
		{
			v2f o = (v2f)0;

			o.color = fixed4(1,0,0,1);
			o.position = UnityWorldToClipPos(float4(0,0,0,1));
			
			return o;
		}

		float4 frag(v2f i) : COLOR
		{
			return i.color;
		}


		ENDCG
		}
	}
	FallBack Off
}
