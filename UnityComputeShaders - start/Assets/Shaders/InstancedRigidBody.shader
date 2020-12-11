Shader "Physics/InstancedRigidBody" { 

   Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_BumpMap ("Bumpmap", 2D) = "bump" {}
		_MetallicGlossMap("Metallic", 2D) = "white" {}
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_Glossiness ("Smoothness", Range(0,1)) = 1.0
	}

   SubShader {
 
		CGPROGRAM

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _MetallicGlossMap;
		struct Input {
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float3 worldPos;
		};
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
 
        #pragma surface surf Standard vertex:vert addshadow nolightmap
        #pragma instancing_options procedural:setup

        float4x4 _Matrix;
        
         #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            struct RigidBody
            {
	            float3 position;
	            float4 quaternion;
	            float3 velocity;
	            float3 angularVelocity;
	            int particleIndex;
	            int particleCount;
            };

            StructuredBuffer<RigidBody> rigidBodiesBuffer; 
         #endif
        
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

            m[3][3] = 1.0;

            return m;
        }

        float4x4 create_matrix(float3 pos, float4 quat){
            float4x4 rotation = quaternion_to_matrix(quat);
			float3 position = pos;
			float4x4 translation = {
				1,0,0,position.x,
				0,1,0,position.y,
				0,0,1,position.z,
				0,0,0,1
			};
			return mul(translation, rotation);
        }

         void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                v.vertex = mul(_Matrix, v.vertex);
            #endif
        }

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                RigidBody body = rigidBodiesBuffer[unity_InstanceID];
                _Matrix = create_matrix(body.position, body.quaternion);
            #endif
        }
 
         void surf (Input IN, inout SurfaceOutputStandard o) {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			fixed4 m = tex2D (_MetallicGlossMap, IN.uv_MainTex); 
			o.Albedo = c.rgb;
			o.Alpha = c.a;
			o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
			o.Metallic = m.r;
			o.Smoothness = _Glossiness * m.a;
         }
 
         ENDCG
   }
}