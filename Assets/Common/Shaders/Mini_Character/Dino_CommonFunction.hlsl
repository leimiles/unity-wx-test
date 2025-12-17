#ifndef DINO_COMMON_FUNCTION_INCLUDED
#define DINO_COMMON_FUNCTION_INCLUDED

float Remap(float value, float inputMin, float inputMax, float outputMin, float outputMax)
{
    return outputMin + (outputMax - outputMin) * ((value - inputMin) / (inputMax - inputMin));
}

float CustomRemap(float input, float minX, float maxX, float minY, float maxY)
{
    return minY + (input - minX) * (maxY - minY) / (maxX - minX);
}

float4 hash4(float2 p)
{
    return frac(sin(float4(1.0 + dot(p, float2(37.0, 17.0)),
                           2.0 + dot(p, float2(11.0, 47.0)),
                           3.0 + dot(p, float2(41.0, 29.0)),
                           4.0 + dot(p, float2(23.0, 31.0)))) * 103.0);
}

// Dissolve_Y Function
half DissolveY(half dissolveTex, half dissolveSmooth, half dissolveThreshold)
{
    half disValue = 1;

    dissolveTex *= dissolveSmooth;
    disValue = saturate(dissolveTex - lerp(dissolveSmooth, -1, dissolveThreshold));
    return disValue;
}

half LinearStep(half minValue, half maxValue, half In)
{
    return saturate((In-minValue) / (maxValue - minValue));
}

half RoughnessToSpecularExponent(half roughness){
    return  sqrt(2 / (roughness + 2));
}

#endif
