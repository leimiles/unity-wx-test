#include "Common.hlsl"
#if !_UNLIT
#define LIT
#endif

#define SPECULAR_POWER_RCP 0.01562 // 1.0/32
#define AIR_RI 1.000293

//Schlick's BRDF fresnel
float ReflectionFresnel(float3 worldNormal, float3 viewDir, float exponent)
{
	float cosTheta = saturate(dot(worldNormal, viewDir));	
	return pow(max(0.0, AIR_RI - cosTheta), exponent);
}

float3 SampleReflections(float3 reflectionVector, float smoothness, float mask, float4 screenPos, float3 positionWS, float3 normal, float3 viewDir, float2 pixelOffset, float2 normalizedScreenSpaceUV)
{
	screenPos.xy += pixelOffset.xy * lerp(1.0, 0.1, unity_OrthoParams.w);
	screenPos /= screenPos.w;
	float3 probe = saturate(GlossyEnvironmentReflection(reflectionVector, 0,smoothness, 1.0, normalizedScreenSpaceUV)).rgb;
	// //Planar reflections are pointless on curved surfaces, skip	
	// float4 planarReflections = SAMPLE_TEXTURE2D_X_LOD(_PlanarReflection, sampler_PlanarReflection, screenPos.xy, 0);
	// //Terrain add-pass can output negative alpha values. Clamp as a safeguard against this
	// planarReflections.a = saturate(planarReflections.a);
	// return lerp(probe, planarReflections.rgb, planarReflections.a * mask);
	return probe;
}

//Single channel overlay
float BlendOverlay(float a, float b)
{
	return (b < 0.5) ? 2.0 * a * b : 1.0 - 2.0 * (1.0 - a) * (1.0 - b);
}

//RGB overlay
float3 BlendOverlay(float3 a, float3 b)
{
	return float3(BlendOverlay(a.r, b.r), BlendOverlay(a.g, b.g), BlendOverlay(a.b, b.b));
}

void AdjustShadowStrength(inout Light light, float strength, float vFace)
{
	light.shadowAttenuation = saturate(light.shadowAttenuation + (1.0 - (strength * vFace)));
}

//In URP light intensity is pre-multiplied with the HDR color, extract via magnitude of color "vector"
float GetLightIntensity(Light light)
{
	return length(light.color);
}

//Specular Blinn-phong reflection in world-space
float3 SpecularReflection(Light light, float3 viewDirectionWS, float3 geometryNormalWS, float3 normalWS, float perturbation, float exponent, float intensity)
{
	//Blend normal
	normalWS = lerp(geometryNormalWS, normalWS, perturbation);
	const float3 halfVec = normalize(light.direction + viewDirectionWS + (normalWS * perturbation));
	half NdotH = saturate(dot(geometryNormalWS, halfVec));
	float specular = pow(NdotH, exponent);
	const float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
	float viewFactor = saturate(dot(geometryNormalWS, light.direction));

	float3 specColor = attenuatedLightColor * specular * intensity * viewFactor;
	
	#if UNITY_COLORSPACE_GAMMA
	specColor = LinearToSRGB(specColor);
	#endif

	return specColor;
}

// UniversalFragmentBlinnPhong
float3 ApplyLighting(inout SurfaceData surfaceData, inout float3 sceneColor, Light mainLight, InputData inputData, WaterSurface water, float shadowStrength, float vFace)
{
	#if _CAUSTICS
	float causticsAttentuation = 1;
	#endif
	
#ifdef LIT
	#if _CAUSTICS && !defined(LIGHTMAP_ON)
	causticsAttentuation = GetLightIntensity(mainLight) * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);
	#endif
	
	AdjustShadowStrength(mainLight, shadowStrength, vFace);
	half3 attenuatedLightColor = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);

	MixRealtimeAndBakedGI(mainLight, water.diffuseNormal, inputData.bakedGI, shadowStrength.xxxx);
	half3 diffuseColor = inputData.bakedGI + LightingLambert(attenuatedLightColor, mainLight.direction, water.diffuseNormal);
	
#if _ADDITIONAL_LIGHTS //Per pixel lights
	#ifndef _SPECULARHIGHLIGHTS_OFF
	// half specularPower = (_PointSpotLightReflectionSize * SPECULAR_POWER_RCP);
	half specularPower = (0 * SPECULAR_POWER_RCP);
	// specularPower = lerp(8.0, 1.0, _PointSpotLightReflectionSize) * _PointSpotLightReflectionStrength;
	specularPower = lerp(8.0, 1.0, 0) * 10;
	#endif
	
	uint pixelLightCount = GetAdditionalLightsCount();
	#if _LIGHT_LAYERS
	uint meshRenderingLayers = GetMeshRenderingLayer();
	#endif
	
	LIGHT_LOOP_BEGIN(pixelLightCount)
		Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowStrength.xxxx);	
		#if _LIGHT_LAYERS
		if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
		#endif
		{
			#if _ADDITIONAL_LIGHT_SHADOWS //URP 11+
			AdjustShadowStrength(light, shadowStrength, vFace);
			#endif

			half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
			diffuseColor += LightingLambert(attenuatedLightColor, light.direction, water.diffuseNormal);

			#ifndef _SPECULARHIGHLIGHTS_OFF
			// surfaceData.specular += SpecularReflection(light, normalize(GetWorldSpaceViewDir(inputData.positionWS)), water.waveNormal, water.tangentWorldNormal, _PointSpotLightReflectionDistortion, lerp(4096, 64, _PointSpotLightReflectionSize), specularPower);
			surfaceData.specular += SpecularReflection(light, normalize(GetWorldSpaceViewDir(inputData.positionWS)), water.waveNormal, water.tangentWorldNormal, 0.5, lerp(4096, 64, 0), specularPower);
			#endif
	}
	LIGHT_LOOP_END
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
	diffuseColor += inputData.vertexLighting;
#endif

#else //Unlit
	const half3 diffuseColor = 1;
#endif

	#if _CAUSTICS
	surfaceData.emission.rgb += water.caustics * causticsAttentuation;
	#endif

	float3 color = (surfaceData.albedo.rgb * diffuseColor) + surfaceData.emission.rgb + surfaceData.specular;
	return color;
}