Shader "Hidden/ShadowBlit"
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
                return o;
            }

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            fixed4 frag (v2f i) : SV_Target
            {
                float dist = tex2D(_CameraDepthTexture, i.uv).r;
                #if !UNITY_REVERSED_Z
                    dist = 1 - dist;
                #endif
                dist = 1 - dist * dist;
                dist = lerp(dist, 1, 0.5);
                
                return fixed4(dist,dist,dist,1);
            }
            ENDCG
        }
    }
}
