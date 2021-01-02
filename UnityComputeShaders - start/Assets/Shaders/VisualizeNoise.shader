Shader "Custom/VisualizeNoise"
{
    Properties
    {
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "noiseSimplex.cginc"
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 position: TEXCOORD1;
            };

            float4 wind;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.position = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 offset = (i.position.xz + wind.xy * _Time.y * wind.z) * wind.w;
                float noise = perlin(offset.x, offset.y);
                return fixed4(noise, noise, noise, 1);
            }
            ENDCG
        }
    }
}
