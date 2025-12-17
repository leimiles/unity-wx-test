#ifndef DINO_VARIATION_INPUT
#define DINO_VARIATION_INPUT

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
half4 _BaseColor;
half4 _EmissionColor;
half4 _BaseMap_ST;
half4 _NormalTex_ST;
half _NormalScale;
half _Smoothness;
half _Metallic;
half _Occlusion;
half _Cutoff;

float _MatcapIntensity;
float _MatcapRotationSpeed;
float _MatcapAddTexIntensity;
float _MatcapAddTexRotationSpeed;

half4 _FresnelColor;
half _FresnelPower;
half _FresnelBias;
half _BreathSpeed;
half _BreathIntensity;
                
// Flow UV properties
float2 _FlowSpeed;
float4 _FlowTilingOffset;

half4 _DitherPack;
half4 _TargetWorldPosition;
CBUFFER_END

TEXTURE2D(_DitherTex);
TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
TEXTURE2D(_NormalTex);   SAMPLER(sampler_NormalTex);
TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_EmissionTex); SAMPLER(sampler_EmissionTex);
TEXTURE2D(_Matcap);      SAMPLER(sampler_Matcap);
TEXTURE2D(_MatcapAddTex);SAMPLER(sampler_MatcapAddTex);
TEXTURE2D(_FlowTex);     SAMPLER(sampler_FlowTex);

#endif
