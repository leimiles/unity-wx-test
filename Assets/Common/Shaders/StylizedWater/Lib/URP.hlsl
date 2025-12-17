#ifndef PIPELINE_INCLUDED
#define PIPELINE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

#ifdef POST_PROCESSING //These would have the core blit library included
#define UNITY_CORE_SAMPLERS_INCLUDED
#endif

#ifndef UNITY_CORE_SAMPLERS_INCLUDED //Backwards compatibility for <2023.1+
#define UNITY_CORE_SAMPLERS_INCLUDED

SamplerState sampler_LinearClamp;
SamplerState sampler_PointClamp;
SamplerState sampler_PointRepeat;
SamplerState sampler_LinearRepeat;
#endif

#ifndef _DISABLE_DEPTH_TEX
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#endif
#endif
