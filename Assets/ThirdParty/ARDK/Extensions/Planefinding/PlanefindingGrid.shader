Shader "Unlit/PlanefindingGrid"
{
  Properties
  {
    _MainTex ("Texture", 2D) = "white" {}
    _MaskTex("Mask", 2D) = "white" {}
        
    // distortion warp stuff
    _ScrollXSpeed("Warp Horizontal Scrolling", Range(-10,10)) = 2
    _ScrollYSpeed("Warp Vertical Scrolling", Range(-10,10)) = 3
    _WarpTex ("Warp Texture", 2D) = "bump" {}
    _WarpStrength ("Warp Strength", float) = 1.0
  }
  SubShader
  {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
    LOD 100
    ZWrite Off
    Blend SrcAlpha OneMinusSrcAlpha

    Pass
    {
      CGPROGRAM
      // Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct appdata members wor)
      // #pragma exclude_renderers d3d11
      #pragma vertex vert
      #pragma fragment frag
      
      #include "UnityCG.cginc"

      struct appdata
      {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
        float2 uv2: TEXCOORD1;
      };

      struct v2f
      {
        float2 uv : TEXCOORD0;
        float2 uv2 :TEXCOORD1;
        float4 vertex : SV_POSITION;
        float2 warpcoord : TEXCOORD3;
      };

      sampler2D _MainTex;
      float4 _MainTex_ST;
      sampler2D _MaskTex;
      float4 _MaskTex_ST;
      fixed _ScrollXSpeed;
      fixed _ScrollYSpeed;
            
      sampler2D _WarpTex;
      float4 _WarpTex_ST;
      fixed _WarpStrength;
      
      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        o.uv2 = TRANSFORM_TEX(v.uv2, _MaskTex);
        o.warpcoord = TRANSFORM_TEX(v.uv, _WarpTex);
        return o;
      }
      
      fixed4 frag (v2f i) : SV_Target
      {
        
        fixed2 scrolledUV = i.warpcoord;
        fixed xScrollValue = _ScrollXSpeed * _Time;
        fixed yScrollValue = _ScrollYSpeed * _Time;
        scrolledUV += fixed2(xScrollValue, yScrollValue);
        fixed4 warp = (tex2D(_WarpTex, scrolledUV) - 0.5) * _WarpStrength;
        
        // sample the texture
        fixed4 col = tex2D(_MainTex, i.uv + warp.rg);
        fixed4 mask = tex2D(_MaskTex, i.uv2);
        
        col.a = mask.r * col.a;
        
        return col;
      }
      ENDCG
    }
  }
}
