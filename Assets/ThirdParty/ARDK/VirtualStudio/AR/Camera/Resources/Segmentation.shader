Shader "ARDK/Segmentation"
{
    Properties
    {
        // The segmentation channel bits. This is set via a MaterialPropertyBlock in each
        // game object that needs to be segmented. See MockSemanticLabel.cs
       [PerRendererData] PackedColor("PackedColor", Color) = (0.0, 0.0, 0.0, 0.0)
    }
    SubShader
    {
        Pass
        {
            Cull Off

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

            float4 PackedColor;

            float4 frag (v2f i) : SV_Target
            {
                return PackedColor;
            }
            ENDCG
        }
    }
}