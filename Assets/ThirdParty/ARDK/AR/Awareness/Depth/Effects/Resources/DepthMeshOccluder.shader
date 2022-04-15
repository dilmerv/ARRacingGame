Shader "ARDK/Effects/DepthMeshOccluder"
{
    Properties
    {
        _textureDepth ("Depth Texture", 2D) = "white" {}
        _textureDepthSuppressionMask ("Depth Suppresion Mask", 2D) = "black" {}
        _colorMask ("Color Mask", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry-1"
        }

        Pass
        {
            ColorMask [_colorMask]
            Offset 1, 1

            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "./ContextAwarenessUtils.cginc"

            // Depth range used for scaling
            float _depthScaleMin;
            float _depthScaleMax;
            
            // Plane samplers
            sampler2D _textureDepth;
            sampler2D _textureDepthSuppressionMask;

            // Transform used to sample the context awareness textures
            float4x4 _depthTransform;
            float4x4 _semanticsTransform;
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };
            
            v2f vert(appdata_base v)
            {
                // Transform UVs
                float4 uv = float4(v.texcoord.x, v.texcoord.y, 1.0f, 1.0f);
                float4 depth_st = mul(_depthTransform, uv);
                float4 semantics_st = mul(_semanticsTransform, uv);
                
                float4 depth_uv = float4(depth_st.x / depth_st.z,  depth_st.y / depth_st.z, 0.0f, 0.0f);
                float4 semantics_uv = float4(semantics_st.x / semantics_st.z,  semantics_st.y / semantics_st.z, 0.0f, 0.0f);

                // Reset depth
                float depth = 0;

                // If depth is not suppressed at this vertex
                if(tex2Dlod(_textureDepthSuppressionMask, semantics_uv).r == 0.0f)
                {
                    // Sample depth
                    depth = tex2Dlod(_textureDepth, depth_uv).r;

                    // Scale depth in case it is normalized
                    depth = depth * (_depthScaleMax - _depthScaleMin) + _depthScaleMin;
                }
                
                v2f o;

                // Write depth
                o.pos.z = DepthToZBufferValue(depth); // see ContextAwarenessUtils.cginc
                o.pos.w = 1.0;

                // Upscale from the mesh's texture space (0..1) to the screen's expected 
                // output domain of -1..1
                o.pos.x = v.texcoord.x * 2.0 - 1.0;
                
                // https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
                float doubleTextCoord = v.texcoord.y * 2.0;
                o.pos.y = lerp
                (
                    1.0 - doubleTextCoord,
                    doubleTextCoord - 1.0,
                    (_ProjectionParams.x + 1) / 2
                );
                                
                // We'll use colors to control debug visualization.
                // The colors that actually get drawn will depend on the ColorMask that's set.
                // R,B correspond to U,V of the disparity texture.
                o.color.x = depth_uv.x;
                o.color.z = depth_uv.y;
                
                // G channel corresponds to raw disparity value.
                o.color.y = depth;
                o.color.w = 1.0f; // For debug visualization, we need opaque.
                
                return o;
            }

            half4 frag(v2f i) : COLOR
            {
                // Use the input color from the vertex, in the event we're using debug visualization.
                return i.color;
            }
            ENDCG
        }
    }
}