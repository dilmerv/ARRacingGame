Shader "Hidden/UnityToMetricDepth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // Flip y-axis because [0,0] is bottom left in Unity textures
                // but top left when surfaced from native
                o.vertex.y = -o.vertex.y;

                return o;
            }

            sampler2D_float _MainTex;

            // Values used to linearize the Z buffer (http://www.humus.name/temp/Linearize%20depth.txt)
            // x = 1-far/near
            // y = far/near
            // z = x/far
            // w = y/far

            // or in case of a reversed depth buffer (UNITY_REVERSED_Z is 1)
            // x = -1+far/near
            // y = 1
            // z = x/far
            // w = 1/far

            float _ZBufferParams_Z;
            float _ZBufferParams_W;

            float frag (v2f i) : SV_Target
            {
                // Pixel values in the _CameraDepthTexture range between
                // 1 (near) and 0 (far), with a non-linear distribution.
                float depth = tex2D(_MainTex, i.uv).r;

                // RFloat textures are not clamped to 0-1, so we can return
                // the actual metric depth even though it can go over 1.0.

                // LinearEyeDepth function duplicated from UnityCG.cginc so
                // _ZBufferParams can be specified unaffected by other cameras.
                float linearEyeDepth = 1.0 / (_ZBufferParams_Z * depth + _ZBufferParams_W);
                return linearEyeDepth;
            }
            ENDCG
        }
    }
}