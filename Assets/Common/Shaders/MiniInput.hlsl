#ifndef MINI_INPUT_INCLUDED
#define MINI_INPUT_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _ST;
    half4 _EmissionColor;
    half4 _MAREConfig;
CBUFFER_END

// this code is used when material property override enabled, must use float4
#ifdef UNITY_DOTS_INSTANCING_ENABLED
    UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _ST)
    UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _MAREConfig)
    UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

    static float4 unity_DOTS_Sampled_BaseColor;
    static float4 unity_DOTS_Sampled_ST;
    static float4 unity_DOTS_Sampled_EmissionColor;
    static float4 unity_DOTS_Sampled_MAREConfig;

    void SetupDOTSLitMaterialPropertyCaches()
    {
        unity_DOTS_Sampled_BaseColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
        unity_DOTS_Sampled_ST = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ST);
        unity_DOTS_Sampled_EmissionColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _EmissionColor);
        unity_DOTS_Sampled_MAREConfig = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _MAREConfig);
    }

    #undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
    #define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSLitMaterialPropertyCaches()

    #define _BaseColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
    #define _ST          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ST)
    #define _EmissionColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _EmissionColor)
    #define _MAREConfig          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _MAREConfig)
#endif

#endif