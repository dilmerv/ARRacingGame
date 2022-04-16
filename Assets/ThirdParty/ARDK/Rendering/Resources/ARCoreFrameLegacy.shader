Shader "Unlit/ARCoreFrameLegacy"
{
 Properties
 {
     _texture ("Texture", 2D) = "black" {}
 }
 
 // For legacy GLES, no depth
 SubShader
{
    Tags { "RenderType"="Opaque" }
    Pass
    {
        Cull Off
        ZWrite Off

        GLSLPROGRAM

        #pragma exclude_renderers gles3

        // #extension GL_OES_EGL_image_external : enable
        
        #ifdef VERTEX

        uniform mat4 _displayTransform;
        varying vec2 _colorUV;

        void main()
        {
            #if defined(SHADER_API_GLES)
            vec4 texCoord = vec4(gl_MultiTexCoord0.x, gl_MultiTexCoord0.y, 0.0f, 1.0f);
            _colorUV = (_displayTransform * texCoord).xy;
            gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
            #endif
        }

        #endif

        #ifdef FRAGMENT
        
        varying vec2 _colorUV;

        // Currently, we render camera images using a cached version of the native texture.
        // We don't require an OES sampler for the cached texture.
        // uniform samplerExternalOES _texture;
        uniform sampler2D _texture;

        void main()
        {
          #if defined(SHADER_API_GLES)
          gl_FragColor = texture(_texture, _colorUV);
          #endif
        }

        #endif

        ENDGLSL
    }
}
    FallBack Off
}
