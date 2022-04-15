Shader "Unlit/ARCoreFrameBiplanar"
{
    Properties
    {
        _textureDepthSuppressionMask ("Depth Suppresion Mask", 2D) = "black" {}
        _textureDepth ("Depth", 2D) = "black" {}
        _textureY ("TextureY", 2D) = "black" {}
        _textureCbCr ("TextureCbCr", 2D) = "gray" {}
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
            ZWrite On
            ZTest Always
            Lighting Off
            LOD 100
            Tags
            {
                "LightMode" = "Always"
            }
            
            GLSLPROGRAM

            #pragma multi_compile_local __ DEPTH_ZWRITE
            #pragma multi_compile_local __ DEPTH_SUPPRESSION
            #pragma multi_compile_local __ DEPTH_DEBUG

            #pragma only_renderers gles3

            #include "UnityCG.glslinc"
            
#ifdef VERTEX

            // Transform used to sample the color planes
            uniform mat4 _displayTransform;

            // Transform used to sample the context awareness textures
            uniform mat4 _depthTransform;
            uniform mat4 _semanticsTransform;

            // Transformed UVs
            varying vec2 _colorUV;
            varying vec3 _depthUV;
            varying vec3 _semanticsUV;

            void main()
            {
                #ifdef SHADER_API_GLES3
                
                // Transform UVs for the color texture
                vec4 texCoord = vec4(gl_MultiTexCoord0.x, gl_MultiTexCoord0.y, 0.0f, 1.0f);
                _colorUV = (_displayTransform * texCoord).xy;

                #ifdef DEPTH_ZWRITE

                // Transform UVs for the context awareness textures
                vec4 uv = vec4(gl_MultiTexCoord0.x, gl_MultiTexCoord0.y, 1.0f, 1.0f);
                _depthUV = (_depthTransform * uv).xyz;

                #ifdef DEPTH_SUPPRESSION
                _semanticsUV = (_semanticsTransform * uv).xyz;
                #endif

                #endif

                // Transform vertex position
                gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
                
                #endif
            }
#endif

#ifdef FRAGMENT
            
            // Transformation used to convert yCbCr color format to RGB
            const mat4 colorTransform = mat4(
                vec4(1.0, 1.0, 1.0, 0.0),
                vec4(0.0, -0.3441, 1.772, 0.0),
                vec4(1.402, -0.7141, 0.0, 0.0),
                vec4(-0.701, 0.5291, -0.886, 1.0)
            );

            // Transformed texture coordinates
            varying vec2 _colorUV;
            varying vec3 _depthUV;
            varying vec3 _semanticsUV;

            // Depth range used for scaling
            uniform float _depthScaleMin;
            uniform float _depthScaleMax;

            uniform sampler2D _textureDepth;
            uniform sampler2D _textureDepthSuppressionMask;
            uniform sampler2D _textureY;
            uniform sampler2D _textureCbCr;

#if defined(SHADER_API_GLES3) && defined(DEPTH_ZWRITE)

            uniform vec4 _ZBufferParams;
            
            // Inverse of LinearEyeDepth
            float EyeDepthToNonLinear(float eyeDepth)
            {   
                return (1.0f - (eyeDepth * _ZBufferParams.w)) / (eyeDepth * _ZBufferParams.z);
            }         
#endif            
            void main()
            {      
#ifdef SHADER_API_GLES3
                
                // Convert the biplanar image to RGB
                vec4 color = colorTransform *
                    vec4(texture(_textureY, _colorUV).x, texture(_textureCbCr, _colorUV).xy, 1.0f);

                // Reset depth
                float depth = 1.0f;

    #ifdef DEPTH_ZWRITE
    #ifdef DEPTH_SUPPRESSION
                    // If depth is not suppressed at this pixel
                    vec2 semanticsUV = vec2(_semanticsUV.x / _semanticsUV.z, _semanticsUV.y / _semanticsUV.z);
                    if (texture(_textureDepthSuppressionMask, semanticsUV).x == 0.0f)
    #endif
                    {
                        // Sample depth
                        vec2 depthUV = vec2(_depthUV.x / _depthUV.z, _depthUV.y / _depthUV.z);
                        float rawDepth = texture(_textureDepth, depthUV).x;

                        // Scale depth in case it is normalized
                        // Note: If depth is not normalized, min and max should
                        // be 0 and 1 respectively to leave the value intact
                        float scaledDepth = rawDepth * (_depthScaleMax - _depthScaleMin) + _depthScaleMin;

                        // Convert depth to z-value and write the zbuffer
                        depth = EyeDepthToNonLinear(scaledDepth);

                        #ifdef DEPTH_DEBUG
                        // Write disparity to the color channels for debug purposes
                        float MAX_VIEW_DISP = 4.0f;
                        float scaledDisparity = 1.0f/scaledDepth;
                        float normDisparity = scaledDisparity/MAX_VIEW_DISP;
                        color = vec4(normDisparity, normDisparity, normDisparity, 1.0f);
                        #endif 
                    }     
    #endif
                gl_FragColor = color;
                gl_FragDepth = depth;
#endif
            }

#endif

            ENDGLSL
        }
    }
    
    Fallback Off
}
