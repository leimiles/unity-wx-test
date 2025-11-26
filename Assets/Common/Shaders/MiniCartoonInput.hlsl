#ifndef MINI_CARTOON_INPUT_INCLUDED
#define MINI_CARTOON_INPUT_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _SpecularColor;
    half4 _HighlightColor;
    half4 _ShadowColor;
    half4 _RimColor;
    half4 _ST;
    half _SpecularShininess;
    half _SpecularGloss;
    half _RampThreshold;
    half _RampSmooth;
    half _RimThreshold;
    half _RimSmooth;
CBUFFER_END

// this code is used when material property override enabled, must use float4
#ifdef UNITY_DOTS_INSTANCING_ENABLED
    UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _SpecularColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _HighlightColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _ShadowColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _RimColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _ST)
    UNITY_DOTS_INSTANCED_PROP(float, _SpecularShininess)
    UNITY_DOTS_INSTANCED_PROP(float, _SpecularGloss)
    UNITY_DOTS_INSTANCED_PROP(float, _RampThreshold)
    UNITY_DOTS_INSTANCED_PROP(float, _RampSmooth)
    UNITY_DOTS_INSTANCED_PROP(float, _RimThreshold)
    UNITY_DOTS_INSTANCED_PROP(float, _RimSmooth)
    UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

    static float4 unity_DOTS_Sampled_BaseColor;
    static float4 unity_DOTS_Sampled_SpecularColor;
    static float4 unity_DOTS_Sampled_HighlightColor;
    static float4 unity_DOTS_Sampled_ShadowColor;
    static float4 unity_DOTS_Sampled_RimColor;
    static float4 unity_DOTS_Sampled_ST;
    static float unity_DOTS_Sampled_SpecularShininess;
    static float unity_DOTS_Sampled_SpecularGloss;
    static float unity_DOTS_Sampled_RampThreshold;
    static float unity_DOTS_Sampled_RampSmooth;
    static float unity_DOTS_Sampled_RimThreshold;
    static float unity_DOTS_Sampled_RimSmooth;

    void SetupDOTSLitMaterialPropertyCaches()
    {
        unity_DOTS_Sampled_BaseColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
        unity_DOTS_Sampled_SpecularColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _SpecularColor);
        unity_DOTS_Sampled_HighlightColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _HighlightColor);
        unity_DOTS_Sampled_ShadowColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ShadowColor);
        unity_DOTS_Sampled_RimColor = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _RimColor);
        unity_DOTS_Sampled_ST = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ST);
        unity_DOTS_Sampled_SpecularShininess = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _SpecularShininess);
        unity_DOTS_Sampled_SpecularGloss = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _SpecularGloss);
        unity_DOTS_Sampled_RampThreshold = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _RampThreshold);
        unity_DOTS_Sampled_RampSmooth = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _RampSmooth);
        unity_DOTS_Sampled_RimThreshold = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _RimThreshold);
        unity_DOTS_Sampled_RimThreshold = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _RimThreshold);
    }

    #undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
    #define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSLitMaterialPropertyCaches()

    #define _BaseColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor)
    #define _SpecularColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _SpecularColor)
    #define _HighlightColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _HighlightColor)
    #define _ShadowColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ShadowColor)
    #define _RimColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _RimColor)
    #define _ST          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _ST)
    #define _SpecularShininess          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _SpecularShininess)
    #define _SpecularGloss          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _SpecularGloss)
    #define _RampThreshold          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _RampThreshold)
    #define _RampSmooth          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _RampSmooth)
    #define _RimThreshold          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _RimThreshold)
    #define _RimThreshold          UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, RimThreshold)
#endif

#endif
