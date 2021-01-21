Shader "Custom/SPHGrid"
{
    Properties
    {
       
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite off

        CGPROGRAM
        #pragma vertex vert
        #pragma surface surf Standard vertex:vert alpha:fade
         
        #include "UnityCG.cginc"

        #ifdef SHADER_METAL_API
            StructuredBuffer<int4> _Grid;
        #endif
        float4 _GridDimensions;
        float4 _GridStartPosition;

        struct Input
        {
            float3 worldPos : TEXCOORD1; // World position
        };

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            data.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        }

        int GetGridIndex(int3 loc) {
	        return loc.x + (_GridDimensions.x * loc.y) + (_GridDimensions.x * _GridDimensions.y * loc.z);
        }

        //Returns pseudo random number in range 0 <= x < 1
        float random(float value, float seed = 0.546){
	        float random = (frac(sin(value + seed) * 143758.5453));// + 1.0)/2.0;
	        return random;
        }

        float raymarch (float3 position, float3 direction)
        {
            float alpha = 0;
            int steps = (int)max(_GridDimensions.x, max(_GridDimensions.y, _GridDimensions.z)) * 2;
            float stepSize = _GridStartPosition.w;
            float stepsInv = (float)1/(float)steps;

            for (int i=0; i<steps; i++)
			{
				int3 loc = (int3)((position.xyz - _GridStartPosition.xyz) / stepSize);
                int index = GetGridIndex(loc);
                if (index>=0 || index<(int)_GridDimensions.w){
                    float texel = 0;
#ifdef SHADER_METAL_API
                    int4 voxel = _Grid[index];
                    if (voxel.x!=-1) texel += 0.25;
                    if (voxel.y!=-1) texel += 0.25;
                    if (voxel.z!=-1) texel += 0.25;
                    if (voxel.w!=-1) texel += 0.25;
                    texel = 1;
                    alpha += texel * stepsInv;
#endif
                }
                
				position += direction * stepSize;
			}
			return alpha;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 viewDirection = normalize(IN.worldPos - _WorldSpaceCameraPos);
            float alpha = raymarch (IN.worldPos, viewDirection);
            o.Albedo = fixed3(1,1,1);
            o.Alpha = alpha;
           
        }
        ENDCG
    }
    FallBack off
}
