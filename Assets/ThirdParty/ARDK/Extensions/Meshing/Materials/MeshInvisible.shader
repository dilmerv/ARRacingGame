Shader "ARDK/Meshing/Invisible" {
    SubShader {
        Tags {"Queue" = "Geometry-10" }
 
        ColorMask 0
        ZWrite On

        Pass {}
    }
}