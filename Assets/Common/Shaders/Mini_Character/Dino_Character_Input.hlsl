#ifndef DINO_CHARACTER_INPUT_INCLUDED
#define DINO_CHARACTER_INPUT_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


CBUFFER_START(UnityPerMaterial)
float4 _AlbedoMap_ST;
half4 _AlbedoColor;
half _Smoothness;
half _Metallic;
half _NormalMapIntensity;
half4 _EmissionColor;
half4 _ST;

half4 _GoochBrightColor;
half4 _GoochDarkColor;
half _RimOn;
half4 _RimColor;
half _RimSize;
half _IridescenceOn;
half _IridescenceSize;
half4 _IridescenceColor;

half _MatcapIntensity;
half _Surface;

half4 _OccludeColor;

half _ShadowAttenuationIntensity;
half _ShadowThreshold;
half _ShadowSmooth;

half4 _DitherPack;
half4 _TargetWorldPosition;
CBUFFER_END

TEXTURE2D(_DitheringTexture);
TEXTURE2D(_DitherTex);
SAMPLER(sampler_DitherTex);

TEXTURE2D(_AlbedoMap);
SAMPLER(sampler_AlbedoMap);
TEXTURE2D(_MetallicMap);
SAMPLER(sampler_MetallicMap);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);

TEXTURE2D(_Matcap);
SAMPLER(sampler_Matcap);


#endif
