Shader "ImageEffect/StarGlow"
{
    Properties
    {
        [HideInInspector]
        _MainTex("Texture", 2D) = "white" {}
        _BrightnessSettings("(Threshold, Intensity, Attenuation, -)", Vector) = (0.8, 1.0, 0.95, 0.0)
    }
    SubShader
    {
        CGINCLUDE

        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4    _MainTex_ST;
        float4    _MainTex_TexelSize;
        float4    _BrightnessSettings;

        #define BRIGHTNESS_THRESHOLD _BrightnessSettings.x
        #define INTENSITY            _BrightnessSettings.y
        #define ATTENUATION          _BrightnessSettings.z

        ENDCG

        // STEP:0
        // Debug.

        Pass
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            fixed4 frag(v2f_img input) : SV_Target
            {
                return tex2D(_MainTex, input.uv);
            }

            ENDCG
        }
    }
}