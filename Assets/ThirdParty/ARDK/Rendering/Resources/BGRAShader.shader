Shader "Unlit/ARDKCameraShaderBGRA"
{
 Properties
 {
     _texture ("Texture", 2D) = "black" {}
 }

// For GLES3 or GLES2
SubShader
{
    Cull Off
    Tags { "RenderType"="Opaque" }

    Pass
    {
        ZWrite Off

        GLSLPROGRAM

        #pragma only_renderers gles3 gles

        #ifdef SHADER_API_GLES3
        #extension GL_OES_EGL_image_external_essl3 : enable
        #else
        #extension GL_OES_EGL_image_external : enable
        #endif

        uniform mat4 _textureTransform;

        #ifdef VERTEX

        varying vec2 textureCoord;

        void main()
        {
            #if defined(SHADER_API_GLES3) || defined(SHADER_API_GLES)
            textureCoord = (_textureTransform * vec4(gl_MultiTexCoord0.x, gl_MultiTexCoord0.y, 0.0f, 1.0f)).xy;
            gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
            #endif
        }

        #endif

        #ifdef FRAGMENT
        varying vec2 textureCoord;
        uniform samplerExternalOES _texture;

        void main()
        {      
          #ifdef SHADER_API_GLES3
          gl_FragColor = texture(_texture, textureCoord);
          #else
          gl_FragColor = textureExternal(_texture, textureCoord);
          #endif
        }

        #endif

        ENDGLSL
    }
}

 SubShader
 {
   Cull Off
   Tags { "RenderType"="Opaque" }
   LOD 100

   Pass
   {
     ZWrite Off
     CGPROGRAM
     #pragma exclude_renderers gles3
     #pragma vertex vert
     #pragma fragment frag
     
     #include "UnityCG.cginc"

     float4x4 _textureTransform;

     struct Vertex {
       float4 position : POSITION;
       float2 texcoord : TEXCOORD0;
     };

     struct TexCoordInOut {
       float4 position : SV_POSITION;
       float2 texcoord : TEXCOORD0;
     };
     
     TexCoordInOut vert (Vertex vertex) {
         TexCoordInOut o;
         o.position = UnityObjectToClipPos(vertex.position);
         o.texcoord = mul(_textureTransform, float4(vertex.texcoord, 0.0f, 1.0f)).xy;
         return o;
     }
     
     // samplers
     sampler2D _texture;

     fixed4 frag (TexCoordInOut i) : SV_Target {
       // sample the texture
       float2 texcoord = i.texcoord;
       return tex2D(_texture, texcoord);
     }
     ENDCG
   }
 }



  FallBack Off
}
