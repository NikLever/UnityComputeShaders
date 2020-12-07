Shader "Physics/ParticleForceLineShader"
    {
        Properties
        {
            _Color ("Color", Color) = (1, 1, 1, 1)
            _LineLength("Float", Float) = 5
        }
     
        SubShader
        {
            Tags { "RenderType"="Opaque" }
            LOD 100
     
            Pass
            {
                CGPROGRAM
                #include "UnityCG.cginc"
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing
                #pragma instancing_options procedural:setup
 
                struct Particle
		        {
			        float3 position;
			        float3 velocity;
			        float3 force;
			        float3 localPosition;
			        float3 offsetPosition;
		        };

                struct appdata
                {
                    float4 vertex : POSITION;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 vertex : SV_POSITION;
                };
     
                float4 _Color;
                float _LineLength;
                float4x4 _Matrix;
                float scale;
               
                StructuredBuffer<Particle> particlesBuffer;

                float4x4 create_matrix(float3 pos) {
                    return float4x4(
                        scale, 0, 0, pos.x,
                        0, scale, 0, pos.y,
                        0, 0, scale, pos.z,
                        0, 0, 0, 1
                    );
                }

                void setup()
                {
                    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                        float3 pos = particlesBuffer[unity_InstanceID].position;
                        _Matrix = create_matrix(pos);
                    #endif
                }
                
                v2f vert(appdata v)
                {
                    v2f o;
     
                    UNITY_SETUP_INSTANCE_ID(v);
                    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                        float4 position = mul(_Matrix, v.vertex);
                   	    o.vertex = UnityObjectToClipPos(position);
                    #else
                        o.vertex = UnityObjectToClipPos(v.vertex);
                    #endif
                    return o;
                }
             
                fixed4 frag(v2f i) : SV_Target
                {
                    return _Color;
                }
                ENDCG
            }
        }
    }