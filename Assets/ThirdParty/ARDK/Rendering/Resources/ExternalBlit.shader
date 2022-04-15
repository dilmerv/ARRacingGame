// Discussion:
// This shader was created to blit EGL textures (external) to 
// regular Unity texture targets on Android platform.
// For context, ARCore performs its camera and pose update within the rendering thread.
// Therefore, we cannot use the external camera textures directly because their contents
// might get overriden during rendering. 
// The currently implemented solution to tackle this issue, is to cache the external
// texture every frame and use the cached version for rendering, because it's guaranteed to be intact.
// Now, this is performed by a Graphics.Blit. It turns out, possibly due to variance in 
// driver implementations, on some devices the built-in blit shader cannot read from external textures.
// This shader therefore is written in glsl, using an OES sampler explicitly to enable 
// blitting on those devices as well. 
// Note that, once ARDK implements the ability to use multiple native textures for its
// camera feed, amongst other things, this shader will become redundant and can be removed. 
Shader "Unlit/ExternalBlit"
{
    Properties
    {
        _texture("Main Texture", 2D) = "white" {}
    }

    SubShader
    {
        Pass
        {
            Cull Off ZWrite Off ZTest Always
            Fog { Mode off }
            
            GLSLPROGRAM

            #ifdef SHADER_API_GLES3
            #extension GL_OES_EGL_image_external_essl3 : require
            #else
            #extension GL_OES_EGL_image_external : enable
            #endif
        
            #ifdef VERTEX

            varying vec2 _textureCoord;

            void main()
            {
                gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
                _textureCoord = gl_MultiTexCoord0.xy;
            }

            #endif

            #ifdef FRAGMENT

            uniform samplerExternalOES _texture;
            varying vec2 _textureCoord;

            void main()
            {
                gl_FragColor = texture(_texture, _textureCoord);
            }

            #endif

            ENDGLSL
        }
    }
    
    Fallback Off
}