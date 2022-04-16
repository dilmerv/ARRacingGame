inline float DepthToZBufferValue(float z)
{
    // this is equivilent to:
    //    float4 pos = mul(UNITY_MATRIX_P, float4(0,0,-z,1));
    //    return pos.z/pos.w;
    return (-z*UNITY_MATRIX_P._m22 + UNITY_MATRIX_P._m23)/z;
}

/**
 * Disparity from the depth buffer is scaled between _minDepth and _maxDepth.
 * Disparities (inverse depths) were predicted in the range 0..1 and correspond to
 * depth predictions between minimum and maximum depth values hard-coded during training
 */
inline float ScaleDisparity(float disparity, float minDepth, float maxDepth)
{
    const float _minDisp = 1 / maxDepth;
    const float _maxDisp = 1 / minDepth;
    
    float nearOffset = 0;//(_ProjectionParams.y - _minDepth);
    float scaledDisp = _minDisp + (_maxDisp - _minDisp) * saturate(disparity);
    return scaledDisp + nearOffset;
}

inline float DisparityToDepth(float disparity, float minDepth, float maxDepth)
{
    return 1.0/lerp(1.0/maxDepth, 1.0/minDepth, saturate(disparity));
}