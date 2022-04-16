Shader "Unlit/ARKitFrame"
{
    Properties
    {
        _textureY ("TextureY", 2D) = "black" {}
        _textureCbCr ("TextureCbCr", 2D) = "gray" {}
        _textureDepth ("Depth", 2D) = "white" {}
        _textureDepthSuppressionMask ("Depth Suppresion Mask", 2D) = "black" {}
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "ForceNoShadowCasting" = "True"
        }

        Pass
        {
            Cull Off
            ZTest Always
            ZWrite On
            Lighting Off
            LOD 100
            Tags
            {
                "LightMode" = "Always"
            }
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_local __ DEPTH_ZWRITE
            #pragma multi_compile_local __ DEPTH_SUPPRESSION
            #pragma multi_compile_local __ DEPTH_DEBUG

            #include "UnityCG.cginc"

            // Transformation used to convert yCbCr color format to RGB
            static const float4x4 colorTransform = float4x4(
                float4(1.0, +0.0000, +1.4020, -0.7010),
                float4(1.0, -0.3441, -0.7141, +0.5291),
                float4(1.0, +1.7720, +0.0000, -0.8860),
                float4(0.0, +0.0000, +0.0000, +1.0000)
            );

            // Transform used to sample the color planes
            float4x4 _displayTransform;

            // Transforms used to sample the context awareness textures
            float4x4 _depthTransform;
            float4x4 _semanticsTransform;

            // Depth range used for scaling
            float _depthScaleMin;
            float _depthScaleMax;

            // Plane samplers
            sampler2D _textureY;
            sampler2D _textureCbCr;
            sampler2D _textureDepth;
            sampler2D _textureDepthSuppressionMask;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 color_uv : TEXCOORD0;
#if DEPTH_ZWRITE
                float3 depth_uv : TEXCOORD1;
#if DEPTH_SUPPRESSION
                float3 semantics_uv : TEXCOORD2;
#endif 
#endif
            };
            
#if DEPTH_ZWRITE

            // Inverse of LinearEyeDepth
            inline float EyeDepthToNonLinear(float eyeDepth, float4 zBufferParam)
            {
	            return (1.0f - (eyeDepth * zBufferParam.w)) / (eyeDepth * zBufferParam.z * 2.0f);
            }
            
#endif

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                // Apply display transform
                o.color_uv = mul(_displayTransform, float4(v.uv, 0.0f, 1.0f)).xy;

                // Transform UVs for the context awareness textures
#if DEPTH_ZWRITE
                o.depth_uv = mul(_depthTransform, float4(v.uv, 1.0f, 1.0f)).xyz;
#if DEPTH_SUPPRESSION
                o.semantics_uv = mul(_semanticsTransform, float4(v.uv, 1.0f, 1.0f)).xyz;
#endif
#endif

                return o;
            }
            
            void frag(in v2f i, out float4 out_color : SV_Target, out float out_depth : SV_Depth)
            {
                // Convert the biplanar image to RGB
                out_color = mul(colorTransform,
                    float4(tex2D(_textureY, i.color_uv).r, tex2D(_textureCbCr, i.color_uv).rg, 1.0f));

                // Clear depth
                out_depth = 0.0f;
                
#if DEPTH_ZWRITE
#if DEPTH_SUPPRESSION
                // If depth is not suppressed at this pixel
                float2 semanticsUV = float2(i.semantics_uv.x / i.semantics_uv.z, i.semantics_uv.y / i.semantics_uv.z);
                if (tex2D(_textureDepthSuppressionMask, semanticsUV).r == 0.0f)
#endif
                {
                    // Sample depth
                    float2 depthUV = float2(i.depth_uv.x / i.depth_uv.z, i.depth_uv.y / i.depth_uv.z);
                    float rawDepth = tex2D(_textureDepth, depthUV).r;

                    // Scale depth in case it is normalized
                    // Note: If depth is not normalized, min and max should
                    // be 0 and 1 respectively to leave the value intact
                    float scaledDepth = rawDepth * (_depthScaleMax - _depthScaleMin) + _depthScaleMin;
                
                    // Convert to nonlinear and write to the zbuffer
                    out_depth = EyeDepthToNonLinear(scaledDepth, _ZBufferParams);
#if DEPTH_DEBUG
                    // Write disparity to the color channels for debug purposes
                    const float MAX_VIEW_DISP = 4.0f;
                    const float scaledDisparity = 1.0f/scaledDepth;
                    const float normDisparity = scaledDisparity/MAX_VIEW_DISP;
                    out_color = float4(normDisparity, normDisparity, normDisparity, 1.0f);
#endif
                }
#endif
            }
            ENDHLSL
        }
    }
}
