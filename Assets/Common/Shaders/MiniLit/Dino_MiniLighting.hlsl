#ifndef DINO_MINI_LIGHTING_INCLUDED
#define DINO_MINI_LIGHTING_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#define invPI   0.3183h
#define INV_FG  0.0625h
#define MEDIUMP_FLT_MAX 65504.0h
#define saturateMediump(x) min(x, MEDIUMP_FLT_MAX)


// half3 MiniGlossyEnvironmentReflection(half perceptualRoughness, half occlusion, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap), half2 matcapUV)
// {
//     #if !defined(_ENVIRONMENTREFLECTIONS_OFF)
//     half3 irradiance;
//     half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
//     // half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip));
//     // irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
//     irradiance = SAMPLE_TEXTURE2D_LOD(_Matcap, sampler_Matcap, matcapUV, mip) * _MatcapIntensity;
//     return irradiance * occlusion;
//     #else
//     return _GlossyEnvironmentColor.rgb * occlusion;
//     #endif // _ENVIRONMENTREFLECTIONS_OFF
// }

half3 MiniGlobalIllumination(BRDFData brdfData,
half3 bakedGI, half occlusion, half3 normalWS, half3 viewDirectionWS, TEXTURE2D_PARAM(matcap, sampler_matcap), half2 matcapUV, half matcapIntensity)
{
    half NoV = saturate(dot(normalWS, viewDirectionWS));
    half fresnelTerm = Pow4(1.0 - NoV);

    half3 indirectDiffuse = bakedGI;
    half3 indirectSpecular = 0;
    
    #if !defined(_ENVIRONMENTREFLECTIONS_OFF)
        half3 irradiance;
        half mip = PerceptualRoughnessToMipmapLevel(brdfData.perceptualRoughness);
        // half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip));
        irradiance = SAMPLE_TEXTURE2D_LOD(matcap, sampler_matcap, matcapUV, mip).rgb * matcapIntensity;
        // half4 encodedIrradiance = SAMPLE_TEXTURE2D_LOD(matcap, sampler_matcap, matcapUV, mip) * matcapIntensity;
        // irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
        indirectSpecular = irradiance * occlusion;
    #else
        indirectSpecular = _GlossyEnvironmentColor.rgb * occlusion;
    #endif // _ENVIRONMENTREFLECTIONS_OFF

    half3 color = EnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);
    
    return color * occlusion;
}

void MiniLightingGeneral(half3 normal, half3 lightDir, half3 viewDir, half3 lightColor, half fZero, half roughness, half ndotv, out half3 outDiffuse, out half3 outSpecular)
{
    // diffuse
    half3 halfVec = normalize(lightDir + viewDir);

    half ndotl = max(dot(normal, lightDir), 0.0h);
    #if defined(LIGHTMAP_ON)
        outDiffuse = 0;
    #else
        outDiffuse = half3(ndotl, ndotl, ndotl) * lightColor;
    #endif

    // specular
    half ndoth = max(dot(normal, halfVec), 0.0h);
    half hdotv = max(dot(viewDir, halfVec), 0.0h);

    half alpha = roughness * roughness;

    half alpha2 = alpha * alpha;
    half sum = ((ndoth * ndoth) * (alpha2 - 1.0h) + 1.0h);
    half denom = PI * sum * sum;
    half D = alpha2 / denom;

    // Compute Fresnel function via Schlick's approximation.
    half fresnel = fZero + (1.0h - fZero) * pow(2.0h, (-5.55473h * hdotv - 6.98316h) * hdotv);
    half k = alpha * 0.5h;

    half G_V = ndotv / (ndotv * (1.0h - k) + k);
    half G_L = ndotl / (ndotl * (1.0h - k) + k);
    half G = (G_V * G_L);

    //half specular = (D * fresnel * G) / (4.0h * ndotv);
    half specular = (D * fresnel * G) / (32.0h * ndotv);        // correction in gamma space

    specular = saturate(specular);
    outSpecular = half3(specular, specular, specular) * lightColor;
}

half3 MiniAdditionalLighting(Light additionalLight, InputData inputData)
{
    half3 halfVec = normalize(additionalLight.direction + inputData.viewDirectionWS);
    half ndotl = max(dot(inputData.normalWS, additionalLight.direction), 0.0h);
    return half3(ndotl, ndotl, ndotl) * additionalLight.color * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
}

struct MiniSurfaceData
{
    half3 albedo;
    half3 normalTS;
    half4 metalic_occlusion_roughness_emissionMask;
    half3 emission;
    half2 matcapUV;
};

void MiniLightingGeneralWithAdditionalLight(BRDFData brdfData, InputData inputData, Light light, half ndotv, out half3 outDiffuse, out half3 outSpecular)
{
    // diffuse
    half3 halfVec = normalize(light.direction + inputData.viewDirectionWS);

    half ndotl = max(dot(inputData.normalWS, light.direction), 0.0h);
    #if defined(LIGHTMAP_ON)
        outDiffuse = 0;
    #else
        outDiffuse = half3(ndotl, ndotl, ndotl) * light.color * light.shadowAttenuation;
        #if defined(_ADDITIONAL_LIGHTS)
            uint pixelLightCount = GetAdditionalLightsCount();
            LIGHT_LOOP_BEGIN(pixelLightCount)
            Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1, 1, 1, 1));
            outDiffuse += MiniAdditionalLighting(additionalLight, inputData);
            // turn off rendering sepcular for now
            LIGHT_LOOP_END
        #endif
    #endif

    // specular
    half ndoth = max(dot(inputData.normalWS, halfVec), 0.0h);
    // half hdotv = max(dot(inputData.viewDirectionWS, halfVec), 0.0h);
    
    half sum = ((ndoth * ndoth) * brdfData.roughness2MinusOne + 1.0h);
    half denom = PI * sum * sum;
    half D = brdfData.roughness2 / denom;

    // half fZero = 1.0h - miniSurfaceData.metalic_occlusion_roughness_emissionMask.r;
    
    // half fZero = 1.0h - brdfData.reflectivity;
    // // Compute Fresnel function via Schlick's approximation.
    // half fresnel = fZero + (1.0h - fZero) * pow(2.0h, (-5.55473h * hdotv - 6.98316h) * hdotv);
    // half k = brdfData.roughness * 0.5h;
    //
    // half G_V = ndotv / (ndotv * (1.0h - k) + k);
    // half G_L = ndotl / (ndotl * (1.0h - k) + k);
    // half G = (G_V * G_L);
    half G = 1;
    half fresnel = 1;

    //half specular = (D * fresnel * G) / (4.0h * ndotv);
    half specular = (D * fresnel * G) / (32.0h * ndotv);        // correction in gamma space

    specular = saturate(specular);
    outSpecular = half3(specular, specular, specular) * light.color;
}

void MiniLightingGeneralWithAdditionalLight(InputData inputData, MiniSurfaceData miniSurfaceData, Light light, half ndotv, out half3 outDiffuse, out half3 outSpecular)
{
    // diffuse
    half3 halfVec = normalize(light.direction + inputData.viewDirectionWS);

    half ndotl = max(dot(inputData.normalWS, light.direction), 0.0h);
    #if defined(LIGHTMAP_ON)
        outDiffuse = 0;
    #else
        outDiffuse = half3(ndotl, ndotl, ndotl) * light.color * light.shadowAttenuation;
        #if defined(_ADDITIONAL_LIGHTS)
            uint pixelLightCount = GetAdditionalLightsCount();
            LIGHT_LOOP_BEGIN(pixelLightCount)
            Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1, 1, 1, 1));
            outDiffuse += MiniAdditionalLighting(additionalLight, inputData);
            // turn off rendering sepcular for now
            LIGHT_LOOP_END
        #endif
    #endif

    // specular
    half ndoth = max(dot(inputData.normalWS, halfVec), 0.0h);
    half hdotv = max(dot(inputData.viewDirectionWS, halfVec), 0.0h);

    half alpha = miniSurfaceData.metalic_occlusion_roughness_emissionMask.b * miniSurfaceData.metalic_occlusion_roughness_emissionMask.b;

    half alpha2 = alpha * alpha;
    half sum = ((ndoth * ndoth) * (alpha2 - 1.0h) + 1.0h);
    half denom = PI * sum * sum;
    half D = alpha2 / denom;

    half fZero = 1.0h - miniSurfaceData.metalic_occlusion_roughness_emissionMask.r;
    // Compute Fresnel function via Schlick's approximation.
    half fresnel = fZero + (1.0h - fZero) * pow(2.0h, (-5.55473h * hdotv - 6.98316h) * hdotv);
    half k = alpha * 0.5h;

    half G_V = ndotv / (ndotv * (1.0h - k) + k);
    half G_L = ndotl / (ndotl * (1.0h - k) + k);
    half G = (G_V * G_L);

    //half specular = (D * fresnel * G) / (4.0h * ndotv);
    half specular = (D * fresnel * G) / (32.0h * ndotv);        // correction in gamma space

    specular = saturate(specular);
    outSpecular = half3(specular, specular, specular) * light.color;
}

half3 CalculateMiniBlinnPhong(Light light, InputData inputData, SurfaceData surfaceData)
{
    half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
    half3 lightDiffuseColor = LightingLambert(attenuatedLightColor, light.direction, inputData.normalWS);

    half3 lightSpecularColor = half3(0, 0, 0);

    half smoothness = exp2(10 * surfaceData.smoothness + 1);
    lightSpecularColor += LightingSpecular(attenuatedLightColor, light.direction, inputData.normalWS, inputData.viewDirectionWS, half4(surfaceData.specular, 1), smoothness);

    #if _ALPHAPREMULTIPLY_ON
        return lightDiffuseColor * surfaceData.albedo * surfaceData.alpha + lightSpecularColor;
    #else
        return lightDiffuseColor * surfaceData.albedo + lightSpecularColor;
    #endif
}

half4 MiniBlinnPhong(InputData inputData, SurfaceData surfaceData)
{
    #if defined(DEBUG_DISPLAY)
        half4 debugColor;

        if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
        {
            return debugColor;
        }
    #endif

    uint meshRenderingLayers = GetMeshRenderingLayer();
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, aoFactor);

    inputData.bakedGI *= surfaceData.albedo;

    LightingData lightingData = CreateLightingData(inputData, surfaceData);
    #ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
    #endif
    {
        lightingData.mainLightColor += CalculateMiniBlinnPhong(mainLight, inputData, surfaceData);
    }

    #if defined(_ADDITIONAL_LIGHTS)
        uint pixelLightCount = GetAdditionalLightsCount();

        #if USE_FORWARD_PLUS
            for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
            {
                FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

                Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                #ifdef _LIGHT_LAYERS
                    if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
                #endif
                {
                    lightingData.additionalLightsColor += CalculateMiniBlinnPhong(light, inputData, surfaceData);
                }
            }
        #endif

        LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
        #ifdef _LIGHT_LAYERS
            if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        #endif
        {
            lightingData.additionalLightsColor += CalculateBlinnPhong(light, inputData, surfaceData);
        }
        LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
        lightingData.vertexLightingColor += inputData.vertexLighting * surfaceData.albedo;
    #endif

    return CalculateFinalColor(lightingData, surfaceData.alpha);
}


struct MiniCartoonSurfaceData
{
    half3 albedo;
    half3 normalTS;
    half4 highlightColor;
    half4 shadowColor;
    half4 rimColor;
    half4 cartoonProps;
    half4 specular;
    half specularGloss;
    half4 shadowCoordBackLight;
};

half Outline(half3 viewDirWS, half3 normalWS)
{
    half ndotv = max(0, dot(viewDirWS, normalWS));
    half rim = (1.0 - ndotv);
    rim = smoothstep(0, 0.1, rim - 0.55);
    //half3 rimColor = rim;
    return rim;
}

void MiniLightingCartoon(InputData inputData, MiniCartoonSurfaceData miniCartoonSurfaceData, Light light, out half3 outDiffuse, out half3 outSpecular)
{
    half ndotl = max(dot(inputData.normalWS, light.direction), 0.0h);
    half ndotvRaw = max(0, dot(inputData.viewDirectionWS, inputData.normalWS));

    // rim lighting
    // half rimMask = smoothstep(miniCartoonSurfaceData.cartoonProps.z, miniCartoonSurfaceData.cartoonProps.w, 1 - ndotvRaw) * ndotl * light.shadowAttenuation;
    // half3 rimColor = rimMask * miniCartoonSurfaceData.rimColor;

    // rim lighting
    half rimTowards;
    //half rampOneMinus = smoothstep(0, miniCartoonSurfaceData.cartoonProps.w, (1 - light.shadowAttenuation) - miniCartoonSurfaceData.cartoonProps.z) * (1 - ndotl);
    #ifdef _RIMTOWARDS_TOWARDSLIGHT
        rimTowards = ndotl * light.shadowAttenuation;
    #elif _RIMTOWARDS_BACKLIGHT
        Light lightBack = GetMainLight(miniCartoonSurfaceData.shadowCoordBackLight);
        half ndotlBack = max(dot(inputData.normalWS, lightBack.direction), 0.0h);
        rimTowards = (1 - ndotlBack) * lightBack.shadowAttenuation;
    #else
        rimTowards = 1;
    #endif

    half rim = (1 - ndotvRaw) * rimTowards;
    rim = smoothstep(0, miniCartoonSurfaceData.cartoonProps.w, rim - miniCartoonSurfaceData.cartoonProps.z);
    half3 rimColor = rim * miniCartoonSurfaceData.rimColor.rgb * light.color;

    // diffuse
    half ramp = smoothstep(0, miniCartoonSurfaceData.cartoonProps.y, ndotl - miniCartoonSurfaceData.cartoonProps.x) * light.shadowAttenuation;
    half3 rampColor = lerp(miniCartoonSurfaceData.shadowColor.rgb, miniCartoonSurfaceData.highlightColor.rgb, ramp);
    outDiffuse = (rampColor * miniCartoonSurfaceData.albedo) * light.color + rimColor;

    // additional light
    #if defined(_ADDITIONAL_LIGHTS)
        uint pixelLightCount = GetAdditionalLightsCount();
        LIGHT_LOOP_BEGIN(pixelLightCount)
        Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1, 1, 1, 1));
        half ndotlAdd = max(dot(inputData.normalWS, additionalLight.direction), 0.0h);
        half rampAdd = smoothstep(0, miniCartoonSurfaceData.cartoonProps.y, ndotlAdd - miniCartoonSurfaceData.cartoonProps.x) * additionalLight.distanceAttenuation * additionalLight.shadowAttenuation;
        half3 rampColorAdd = lerp(miniCartoonSurfaceData.shadowColor.rgb, miniCartoonSurfaceData.highlightColor.rgb, rampAdd);
        outDiffuse += rampColorAdd * additionalLight.color * miniCartoonSurfaceData.albedo;
        // turn off rendering sepcular for now
        LIGHT_LOOP_END
    #endif

    // specular
    half3 halfVec = normalize(light.direction + inputData.viewDirectionWS);
    half ndoth = max(dot(inputData.normalWS, halfVec), 0.0h);

    half spec = pow(ndoth, miniCartoonSurfaceData.specular.a * 128.0) * miniCartoonSurfaceData.specularGloss * light.shadowAttenuation;
    spec = smoothstep(0, miniCartoonSurfaceData.cartoonProps.y, spec - miniCartoonSurfaceData.cartoonProps.x);

    outSpecular = half3(spec, spec, spec) * miniCartoonSurfaceData.specular.rgb * light.color;
}

#endif