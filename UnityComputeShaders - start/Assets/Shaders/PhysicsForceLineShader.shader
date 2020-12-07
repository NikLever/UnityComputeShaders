    Shader "Physics/ParticleForceLineShader"
    {
        Properties
        {
            _Color ("Color", Color) = (1, 1, 1, 1)
            _LineLength("Velocity Line Scaler", Float) = 50
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
     			void setup() {}

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
               
                StructuredBuffer<Particle> particlesBuffer;
                
                v2f vert(appdata v)
                {
                    v2f o;
     
                    UNITY_SETUP_INSTANCE_ID(v);
                    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                        int instance_id = UNITY_GET_INSTANCE_ID(v);
                        Particle particle = particlesBuffer[instance_id];
                        float3 position = particle.position;
                        float3 endPoint = particle.velocity * _LineLength * v.vertex; 
                   	    o.vertex = UnityObjectToClipPos(position + endPoint);
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
