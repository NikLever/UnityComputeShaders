Shader "Custom/GrassBlades"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags{ "RenderType"="Opaque" }
        
		LOD 200
		Cull Off
		
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types   
        #pragma surface surf Standard vertex:vert addshadow fullforwardshadows
        #pragma instancing_options procedural:setup

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _Scale;
        float _Fade;
        float4x4 _Matrix0;
        float4x4 _Matrix1;
        float4x4 _Matrix2;
        float4x4 _Matrix3;
        float4x4 _Matrix4;

        float4x4 create_matrix(float3 pos, float theta){
            float c = cos(theta);
            float s = sin(theta);
            return float4x4(
                1, 0,  0, pos.x,
                0, c, -s, pos.y,
                0, s,  c, pos.z,
                0, 0,  0, 1
            );
        }
        
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            struct GrassBlade
            {
                float3 position;
                float lean;
                float noise;
                float fade;
            };
            StructuredBuffer<GrassBlade> bladesBuffer; 
        #endif

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                uint vID = (uint)v.texcoord1[0];
                v.vertex.xyz *= _Scale;
                if (vID==2 || vID==3){
                    v.vertex = mul(_Matrix1, v.vertex);
                }else if (vID==4 || vID==5){
                    v.vertex = mul(_Matrix2, v.vertex);
                }else if (vID==6 || vID==7){
                    v.vertex = mul(_Matrix3, v.vertex);
                }else if (vID==8){
                    v.vertex = mul(_Matrix4, v.vertex);
                }else{
                    v.vertex = mul(_Matrix0, v.vertex);
                }
            #endif
        }

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                GrassBlade blade = bladesBuffer[unity_InstanceID];
                _Matrix4 = create_matrix(blade.position, blade.lean);
                _Matrix3 = create_matrix(blade.position, blade.lean*0.7);
                _Matrix2 = create_matrix(blade.position, blade.lean*0.3);
                _Matrix1 = create_matrix(blade.position, blade.lean*0.1);
                _Matrix0 = create_matrix(blade.position, 0);
                _Fade = blade.fade;
            #endif
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color * _Fade;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
