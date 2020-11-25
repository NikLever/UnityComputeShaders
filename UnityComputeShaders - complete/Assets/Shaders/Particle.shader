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

		struct Particle{
			float3 position;
			float3 velocity;
			float life;
		};
		
		struct v2f{
			float4 position : SV_POSITION;
			float4 color : COLOR;
			float life : LIFE;
			float size: PSIZE;
		};
		// particles' data
		StructuredBuffer<Particle> particleBuffer;
		

		v2f vert(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
		{
			v2f o = (v2f)0;

			// Color
			float life = particleBuffer[instance_id].life;
			float lerpVal = life * 0.25f;
			o.color = fixed4(1.0f - lerpVal+0.1, lerpVal+0.1, 1.0f, lerpVal);

			// Position
			o.position = UnityObjectToClipPos(float4(particleBuffer[instance_id].position, 1.0f));
			o.size = _PointSize;

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
