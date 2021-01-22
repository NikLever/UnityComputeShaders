Shader "Custom/SPHParticle"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _EnvironmentMap("Environment Map", CUBE ) = "cube" {}
        _ReflectionStrength("Reflection Strength", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types  
        #pragma surface surf Standard vertex:vert fullforwardshadows
        #pragma instancing_options procedural:setup

        // Use shader model 3.0 target, to get nicer looking lighting 
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldRefl;
        };

        half _Glossiness;
        half _Metallic;
        float _Radius;
        float3 _Position;
        fixed4 _Color;
        samplerCUBE _EnvironmentMap;
        float _ReflectionStrength;

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            struct SPHParticle
            {
                float3 position;
                float3 velocity;
                float3 force;
                float density;
                float pressure;
            };

            StructuredBuffer<SPHParticle> particles;
        #endif

        UNITY_INSTANCING_BUFFER_START(Props)
            
        UNITY_INSTANCING_BUFFER_END(Props)

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                SPHParticle particle = particles[unity_InstanceID];
                _Position = particle.position;
            #endif
        }

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                v.vertex.xyz *= _Radius;
                v.vertex.xyz += _Position;
            #endif
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _Color.rgb;
            o.Emission = texCUBE (_EnvironmentMap, IN.worldRefl).rgb * _ReflectionStrength;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}