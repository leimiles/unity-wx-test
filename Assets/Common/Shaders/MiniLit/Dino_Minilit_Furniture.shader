Shader "Dino/Mini/MiniLit_Furniture"
{
    Properties
    {
		[Enum(Zero, 0, Occ, 3)]_Stencil ("Stencil ID", float) = 3
        _StencilZFail ("Stencil ZFail", Int) = 0
    	
        // uv1
        _BaseMap ("固有色", 2D) = "white" {}
    	
        // uv0
        _AOMap ("AO图", 2D) = "white" {}
        
    	// uv2
        _DecalMap ("纹样图", 2D) = "black" {}
    	_colorDodgeClamp("纹样透明度", Range(0, 1)) = 0.7

        _Metallic ("金属度", Range(-0.5, 1.5)) = 0 
        _Roughness ("粗糙度", Range(-0.5, 1.5)) = 0.5
        
        _SpecShininess ("高光收束值", Float) = 200
        _SpecIntensity("高光强度", float) = 1
        [HDR]_EmissionColor("自发光颜色",Color) = (0.5,0.5,0.5,1)
        
    	//暂时关闭IBL
//    	_EnvMap ("Emission Map", 2D) = "black" {}
//        _TintExpose("IBL Exposure", Float) = 1.0
//        _Rotate("IBL Rotation", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue" = "Geometry+15"}
        LOD 100

        Pass
        {
            Name "Forward"
			Tags { "LightMode" = "UniversalForward" }
			Stencil
            {
                Ref [_Stencil]
                Comp Always
                Pass Replace
                Fail Keep
                ZFail [_StencilZFail]
            }
			
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _SHADOWS_SOFT
			// #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			// #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            
            #define UNITY_PI  3.14159265359f

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv0 : TEXCOORD0; 
                float2 uv1 : TEXCOORD1; 
                float2 uv2 : TEXCOORD2;
            };

            struct v2f
            {
                float2 uv0 : TEXCOORD0; 
                float2 uv1 : TEXCOORD1; 
                float2 uv2 : TEXCOORD2;
                
                float4 vertex : SV_POSITION;
                float3 vertexSH : TEXCOORD3; 
                float3 normalWS : TEXCOORD4;
                float3 worldPos : TEXCOORD5;
            };

			inline half OneMinusReflectivityMetallic_Custom(half4 DieletricSpec, half metallic)
			{			
				half oneMinusDielectricSpec = DieletricSpec.a;
				return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
			}

            CBUFFER_START(UnityPerMaterial)
            half _SpecShininess;   
            half _SpecIntensity;
            
            half _Metallic;
            half _Roughness;
            
            half4 _BaseMap_ST;
            half4 _EmissionMap_ST;
            half4 _AOMap_ST;
            half4 _DecalMap_ST;
			half _DecalBlendFactor;
			half _colorDodgeClamp;
            
            half4 _EmissionColor;
            
            //IBL;
            half4 _EnvMap_HDR;
            half _TintExpose;
            half _Rotate;
            CBUFFER_END

            TEXTURE2D(_BaseMap);     SAMPLER(sampler_BaseMap);
			TEXTURE2D(_AOMap);          SAMPLER(sampler_AOMap);
			TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
            TEXTURECUBE(_EnvMap);       SAMPLER(sampler_EnvMap);
            TEXTURE2D(_DecalMap);       SAMPLER(sampler_DecalMap);
           
            float3 RotateAround(float degree, float3 target)
            {
                float rad = degree * UNITY_PI / 180;
                float2x2 m_rotate = float2x2(cos(rad),-sin(rad),
                                             sin(rad),cos(rad));
                float2 dir_rotate = mul(m_rotate, target.xz);
                target = float3(dir_rotate.x, target.y, dir_rotate.y);
                return target;
            }

            v2f vert (appdata v)
            {
                v2f o = (v2f)0;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = vertexInput.positionCS;
                
                o.uv0 = TRANSFORM_TEX(v.uv0, _AOMap);
                o.uv1 = TRANSFORM_TEX(v.uv1, _BaseMap); // Albedo and Emission share UV1
                o.uv2 = TRANSFORM_TEX(v.uv2, _DecalMap);

                o.normalWS = TransformObjectToWorldNormal(v.normal);
                o.worldPos = vertexInput.positionWS;

				OUTPUT_SH(o.normalWS, o.vertexSH);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
				half4 mainTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv1);
				half aoTex = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, i.uv0).r;
				half4 decalTex = SAMPLE_TEXTURE2D(_DecalMap, sampler_DecalMap, i.uv2);
				
				half ao = aoTex;
				half3 emissionColor = mainTex.rgb * _EmissionColor.rgb;

                half metal = saturate(_Metallic);
                half roughness = saturate(_Roughness);
                
                float3 bump = normalize(i.normalWS);
            	
				half4 shadowMask = unity_ProbesOcclusion;
            	
                float4 SHADOW_COORDS = TransformWorldToShadowCoord(i.worldPos);
				Light light = GetMainLight(SHADOW_COORDS, i.worldPos, shadowMask);
				half3 lightDir = light.direction;
				half3 lightColor = light.color;
				half lightAtten = light.distanceAttenuation;
				half shadowAtten = MainLightShadow(SHADOW_COORDS, i.worldPos, shadowMask, _MainLightOcclusionProbes);
				float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

                half3 base_color = mainTex.rgb ;   
                half3 spec_color = lerp(0.04, base_color , metal); 

                //  Direct diffuse  直接光漫反射
                half NdotL = dot(bump, lightDir); 
                half clampedNdotL = saturate(NdotL);
            	half diff_term = clampedNdotL;
                half half_lambert = (diff_term + 1.0) * 0.5;
                half oneMinusReflectivity = OneMinusReflectivityMetallic_Custom(kDielectricSpec, metal);
				half3 brdfDiffuse = diff_term * lightColor * base_color * lightAtten * oneMinusReflectivity;
                half3 direct_diffuse = brdfDiffuse* half_lambert;

                //  Direct Specular  直接光镜面反射
                half3 halfDir = normalize(lightDir + viewDir);
                half NdotH = dot(bump, halfDir);
                half smoothness = 1.0 - roughness;
                half shininess = lerp(1, _SpecShininess, smoothness * smoothness);
                half spec_term = pow(max(0.0, NdotH), shininess) * _SpecIntensity;
                half3 direct_specular = spec_term * base_color * lightColor * lightAtten * clampedNdotL;
                
                //  Indirect diffuse  间接光漫反射
				half3 bakedGI = SAMPLE_GI(float2(0,0), i.vertexSH, bump);
                float3 env_diffuse = bakedGI * ao * base_color * half_lambert;

                //  Indirect Specular 间接光镜面反射
                // half3 reflect_dir = reflect(-viewDir, bump);
                // reflect_dir = RotateAround(_Rotate, reflect_dir);
                //
                // roughness = roughness * (1.7 - 0.7 * roughness);
                // float mip_level = roughness * 6.0;
                // half4 color_cubemap = SAMPLE_TEXTURECUBE_LOD(_EnvMap, sampler_EnvMap, reflect_dir, mip_level);
                // half3 env_color = DecodeHDREnvironment(color_cubemap, _EnvMap_HDR);
                // float3 env_specular = env_color * _TintExpose * spec_color * ao * metal;
                //
                // float3 final_color = (direct_diffuse + direct_specular) * shadowAtten + env_diffuse + env_specular;
            	float3 final_color = (direct_diffuse + direct_specular) * shadowAtten + env_diffuse ;

        	    //点光源
    //             #ifdef _ADDITIONAL_LIGHTS
				// uint addLightsCount = GetAdditionalLightsCount();
				// for (uint idx = 0; idx < addLightsCount; idx++)
				// {
				// 	Light addLight = GetAdditionalLight(idx, i.worldPos);
				// 	float3 addLightDir = addLight.direction;			
				// 	float addLightShadow = addLight.shadowAttenuation;
    //                 half3 addLightColor = addLight.color;
    //                 half addLightAtten = addLight.distanceAttenuation;
    //
    //                 half NdotL_add = saturate(dot(bump, addLightDir));
    //                 half diff_term_add = max(0.0, NdotL_add);
    //                 half half_lambert_add = (diff_term_add + 1.0) * 0.5;
    //                 half oneMinusReflectivity_add = OneMinusReflectivityMetallic_Custom(kDielectricSpec, metal);
    //                 half3 brdfDiffuse_add = base_color * oneMinusReflectivity_add;
    //
    //                 half3 addDirect_diffuse = brdfDiffuse_add * half_lambert_add * addLightColor * addLightAtten;
    //
    //                 half3 halfDir_add = normalize(addLightDir + viewDir);
    //                 half NdotH_add = dot(bump, halfDir_add);
    //                 half shininess_add = lerp(1, _SpecShininess, smoothness * smoothness);
    //                 half spec_term_add = pow(max(0.0, NdotH_add), shininess_add) * _SpecIntensity;
    //                 half3 addDirect_specular = spec_term_add * base_color * addLightColor * addLightAtten;
    //                 
    //                 final_color = final_color + addDirect_specular + addDirect_diffuse;
				// }
    //             #endif
            	
				half3 decalBlended = final_color / (1.0 - decalTex.rgb * _colorDodgeClamp);	
				half3 FinalColor = decalBlended + emissionColor;
                return float4(FinalColor, 1.0);
            }
            ENDHLSL
        }
        
        Pass
		{
			Name "ShadowCaster"
			Tags{"LightMode" = "ShadowCaster"}

			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull[_Cull]

			HLSLPROGRAM
			#pragma target 2.0	
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma multi_compile_instancing
			#pragma multi_compile _ DOTS_INSTANCING_ON
			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment

			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
			ENDHLSL
		}
    }
}
