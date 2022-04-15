Shader "ARDK/Effects/DepthMeshOcclusionEffect"
{
    Properties
    {
        _minDepth("Min Depth", Float) = 0.0
        _maxDepth("Max Depth", Float) = 0.0
        _disparityTexture ("Depth Texture", 2D) = "white" {}
        _suppressionTexture ("Suppression Texture", 2D) = "black" {}
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

            sampler2D_float _disparityTexture;
            sampler2D_float _suppressionTexture;
            float _minDepth;
            float _maxDepth;
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            /**
             * This function is called for every vertex in the mesh, i.e.,
             * for every pixel in the depthmap
             *
             * \param[in] v.texcoord Texture coordinates (0..1) of this vertex in the mesh
             * \param[in] _depthTexture A 2D texture sampler that can read disparity values (0..1) from
             *           the 1 channel depth texture
             * \return o o.pos contains the 3D camera space position of the mesh's vertex derived from the depthmap
             */
            v2f vert(appdata_base v)
            {
                float2 depthUV = float2(v.texcoord.x,  v.texcoord.y);

                float disparity = tex2Dlod(_disparityTexture, saturate(float4(depthUV, 0.0, 0.0))).r;
                float scaledDisparity = ScaleDisparity(disparity, _minDepth, _maxDepth); // see ContextAwarenessUtils.cginc
                float depth = DisparityToDepth(disparity, _minDepth, _maxDepth); // see ContextAwarenessUtils.cginc

                // If this suppression texture pixel is white, the pixel belongs to the channel
                // NOTE : From what I understand the pixel should be either 0.0 or 1.0.
                // Could possibly use multiplication instead of branching? Need to confirm.
                if(tex2Dlod(_suppressionTexture, saturate(float4(depthUV, 0.0, 0.0))).r > 0.0)
                {
                    // Suppress it
                    depth = _maxDepth;
                }

                // Remap the 70-100 range to the 70-1000 range with exponential distance increase
                // The logic behind this is that currently, depth models become especially noisy
                // in this range. So we want to push the noise away to make a cleaner occlusion.
                // When the model becomes more stable at further distances, this can be removed.
                depth += pow(max(0.0, (depth - 70.0)), 2.0);

                v2f o;
                o.pos.z = DepthToZBufferValue(depth); // see ContextAwarenessUtils.cginc
                o.pos.w = 1.0;

                // Upscale from the mesh's texture space (0..1) to the screen's expected output domain of -1..1
                o.pos.x = v.texcoord.x * 2.0 - 1.0;

                // https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
                float doubleTextCoord = v.texcoord.y * 2.0;
                o.pos.y = lerp(1.0 - doubleTextCoord, doubleTextCoord - 1.0, (_ProjectionParams.x + 1) / 2);

                // We'll use colors to control debug visualization.
                // The colors that actually get drawn will depend on the ColorMask that's set.
                // R,B correspond to U,V of the disparity texture.
                o.color.x = depthUV.x;
                o.color.z = depthUV.y;
                // G channel corresponds to raw disparity value.
                o.color.y = scaledDisparity;
                o.color.w = 1.0f; // For debug visualiztion, we need opaque.
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