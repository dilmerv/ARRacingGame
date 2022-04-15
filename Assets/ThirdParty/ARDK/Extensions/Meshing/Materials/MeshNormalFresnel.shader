Shader "ARDK/Meshing/NormalFresnel" {
    Properties {
        _Alpha ("Alpha", Range(0,1)) = 0.33
        _DistanceClose ("Distance Close", Range(0,10)) = 1.0
        _DistanceFar ("Distance Far", Range(0,30)) = 15.0
		_FresnelColor ("Fresnel Color", Color) = (1,1,1,1)
		_FresnelBias ("Fresnel Bias", Range(-3,3)) = 0
		_FresnelScale ("Fresnel Scale", Range(0,2)) = 1
		_FresnelPower ("Fresnel Power", Range(-10,10)) = 1
    }
    SubShader {
        Tags { "Queue"="Geometry-10" "RenderType"="Transparent" }
        LOD 100

        Pass {
            ZWrite On
            ColorMask 0
        }

        Pass {
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #pragma vertex vert
            #pragma fragment frag
         
            struct v2f {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR0; // normal color
				float fresnel : TEXCOORD0; // fresnel value
            };

            fixed _Alpha;
            half _DistanceClose;
            half _DistanceFar;
			fixed4 _FresnelColor;
			fixed _FresnelBias;
			fixed _FresnelScale;
			fixed _FresnelPower;
            
            v2f vert (appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // calculate the fresnel factor.
                float3 i = normalize(ObjSpaceViewDir(v.vertex));
				o.fresnel = _FresnelBias + _FresnelScale * pow(1 + dot(i, v.normal), _FresnelPower);
                // map geometry normal to cyan-magenta-yellow
                half3 normal = half3(1,1,1) - abs(UnityObjectToWorldNormal(v.normal));
                // calculate distance factor (0 = close, 1 = far)
                half closeFar = max(0.1, _DistanceFar - _DistanceClose);
                half vertexDist = length(ObjSpaceViewDir(v.vertex));
                half l = clamp((vertexDist - _DistanceClose) / closeFar, 0.0, 1.0);
                // set the color as the normal CMY + distance transparency factor 
                o.color = half4(normal,  lerp(_Alpha, 0.0, l));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 f = lerp(_FresnelColor, i.color, i.fresnel);
                return f;
            }
            ENDCG
        }
    } 
}