#ifndef MINI_LIGHTING_INCLUDED
#define MINI_LIGHTING_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#define invPI   0.3183h
#define INV_FG  0.0625h
#define MEDIUMP_FLT_MAX 65504.0h
#define saturateMediump(x) min(x, MEDIUMP_FLT_MAX)

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
};

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