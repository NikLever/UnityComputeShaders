Shader "Custom/GrassTrample"
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
        float _Trample;
        float4x4 _Matrix;
        float4x4 _TrampleMatrix;
        float3 _Position;

        float4x4 quaternion_to_matrix(float4 quat)
        {
            float4x4 m = float4x4(float4(0, 0, 0, 0), float4(0, 0, 0, 0), float4(0, 0, 0, 0), float4(0, 0, 0, 0));

            float x = quat.x, y = quat.y, z = quat.z, w = quat.w;
            float x2 = x + x, y2 = y + y, z2 = z + z;
            float xx = x * x2, xy = x * y2, xz = x * z2;
            float yy = y * y2, yz = y * z2, zz = z * z2;
            float wx = w * x2, wy = w * y2, wz = w * z2;

            m[0][0] = 1.0 - (yy + zz);
            m[0][1] = xy - wz;
            m[0][2] = xz + wy;

            m[1][0] = xy + wz;
            m[1][1] = 1.0 - (xx + zz);
            m[1][2] = yz - wx;

            m[2][0] = xz - wy;
            m[2][1] = yz + wx;
            m[2][2] = 1.0 - (xx + yy);

            m[0][3] = _Position.x;
            m[1][3] = _Position.y;
            m[2][3] = _Position.z;
            m[3][3] = 1.0;

            return m;
        }

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
            struct GrassClump
            {
                float3 position;
                float lean;
                float trample;
                float4 quaternion;
                float noise;
            };
            StructuredBuffer<GrassClump> clumpsBuffer; 
        #endif

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                v.vertex.xyz *= _Scale;
                float4 rotatedVertex = mul(_Matrix, v.vertex);
                float4 trampledVertex = mul(_TrampleMatrix, v.vertex);
                v.vertex.xyz += _Position;
                trampledVertex = lerp(v.vertex, trampledVertex, v.texcoord.y);
                v.vertex = lerp(v.vertex, rotatedVertex, v.texcoord.y);
                v.vertex = lerp(v.vertex, trampledVertex, _Trample);
            #endif
        }

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                GrassClump clump = clumpsBuffer[unity_InstanceID];
                _Trample = clump.trample;
                _Position = clump.position;
                _Matrix = create_matrix(clump.position, clump.lean);
                _TrampleMatrix = quaternion_to_matrix(clump.quaternion);       
            #endif
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            clip(c.a-0.4);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
