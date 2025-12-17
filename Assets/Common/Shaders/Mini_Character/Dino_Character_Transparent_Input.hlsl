#ifndef DINO_CHARACTER_TRANSPARENT_INPUT_INCLUDED
#define DINO_CHARACTER_TRANSPARENT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
float4 _AlbedoMap_ST;
half4 _AlbedoColor;
half4 _SpecColor;
half4 _EmissionColor;
half _Cutoff;
half _Surface;

half4 _GoochBrightColor;
half4 _GoochDarkColor;

half4 _DitherPack;
half4 _TargetWorldPosition;
CBUFFER_END

TEXTURE2D(_AlbedoMap);
SAMPLER(sampler_AlbedoMap);

TEXTURE2D(_DitheringTexture);
TEXTURE2D(_DitherTex);
SAMPLER(sampler_DitherTex);

inline void InitializeSimpleLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;

    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_AlbedoMap, sampler_AlbedoMap));
    outSurfaceData.alpha = albedoAlpha.a * _AlbedoColor.a;
    outSurfaceData.alpha = AlphaDiscard(outSurfaceData.alpha, _Cutoff);

    outSurfaceData.albedo = albedoAlpha.rgb * _AlbedoColor.rgb;
    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);

    outSurfaceData.metallic = 0.0; // unused
    outSurfaceData.specular = _SpecColor.rgb;
    outSurfaceData.smoothness = _SpecColor.a;
    outSurfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
    outSurfaceData.occlusion = 1.0;
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
}

#endif
