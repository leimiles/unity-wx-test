Shader "SoFunny/Mini/MiniLit"
{
    Properties
    {
        // override properties, this shader don't support alphaclip
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset]_BaseMap ("Base Map", 2D) = "white" { }
        [NoScaleOffset]_NormalMap ("Normal Map", 2D) = "bump" { }
        [NoScaleOffset]_MAREMap ("Non-Metallic AO Roughness EmissiveMask Map", 2D) = "white" { }
        [NoScaleOffset] _MAREConfig ("MARE Configure", Vector) = (1, 1, 1, 1)
        [HDR]_EmissionColor ("Emission Color", Color) = (0, 0, 0, 0)
        _ST ("Scale And Offset", Vector) = (1, 1, 0, 0)
        [Toggle(_ADDITIONAL_LIGHTS)] _AddLightOn ("Additional Light On", Float) = 0

        // Stencil properties
        [HideInInspector]_StencilRef ("Stencil Reference", Float) = 0
        [HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8 // Always
        [HideInInspector]_StencilPass ("Stencil Pass", Float) = 0 // Keep
        [HideInInspector]_StencilFail ("Stencil Fail", Float) = 0 // Keep
        [HideInInspector]_StencilZFail ("Stencil ZFail", Float) = 0 // Keep

        [HideInInspector] _ZTest ("ZTest", Float) = 4         // 4 => LEqual
        [HideInInspector] _ZWrite ("ZWrite", Float) = 1       // 1 => On, 0 => Off
        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1  // 1 => One
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0  // 0 => Zero

    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            ZTest [_ZTest]
            ZWrite [_ZWrite]
            Blend [_SrcBlend] [_DstBlend]

            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }

            HLSLPROGRAM
            #pragma target 2.0
            #include "MiniInput.hlsl"
            #include "MiniLighting.hlsl"

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma shader_feature_local _ _ADDITIONAL_LIGHTS
            #pragma shader_feature _ Debug_Albedo Debug_Normal Debug_Metallic Debug_AO Debug_Roughness Debug_Emission Debug_Light Debug_BakedGI

            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ ENABLE_VS_SKINNING
            #pragma multi_compile_instancing

            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED

            #pragma vertex vert
            #pragma fragment frag


            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                half4 tangentOS : TANGENT;
                half2 texcoord0 : TEXCOORD0;
                half2 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 uv0uv1 : TEXCOORD0;
                half4 normalWS : TEXCOORD2;     // w = viewDir.x
                half4 tangentWS : TEXCOORD3;    // w = viewDir.y
                half4 bitangentWS : TEXCOORD4;  // w = viewDir.z
                half4 sh_tangentSign : TEXCOORD5;
                float3 positionWS : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);        SAMPLER(sampler_NormalMap);
            TEXTURE2D(_MAREMap);        SAMPLER(sampler_MAREMap);

            inline void InitializeMiniSurfaceData(half2 uv, out MiniSurfaceData outMiniSurfaceData)
            {
                outMiniSurfaceData = (MiniSurfaceData)0;
                outMiniSurfaceData.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * _BaseColor.rgb;
                outMiniSurfaceData.normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv));
                outMiniSurfaceData.metalic_occlusion_roughness_emissionMask = SAMPLE_TEXTURE2D(_MAREMap, sampler_MAREMap, uv);
                outMiniSurfaceData.metalic_occlusion_roughness_emissionMask = LinearToSRGB(outMiniSurfaceData.metalic_occlusion_roughness_emissionMask * _MAREConfig);
            }

            void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
            {
                inputData = (InputData)0;
                inputData.positionWS = input.positionWS;   // no need for now
                half3 viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
                half sign = input.sh_tangentSign.w;
                input.bitangentWS.xyz = sign * (cross(input.normalWS.xyz, input.tangentWS.xyz));
                inputData.tangentToWorld = half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
                inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
                inputData.viewDirectionWS = SafeNormalize(viewDirWS);     // do it in vertex stage

                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);      // just because we only need shadow cascade situation

                //inputData.fogCoord = 0; //    no need for now
                //inputData.vertexLighting = 0    // no need for now
                #if defined(LIGHTMAP_ON)
                    inputData.bakedGI = SampleLightmap(input.uv0uv1.zw, 0, inputData.normalWS);
                #else
                    inputData.bakedGI = SampleSHPixel(input.sh_tangentSign.xyz, inputData.normalWS);
                #endif
                inputData.bakedGI = LinearToSRGB(inputData.bakedGI);        // only for minirp
                //inputData.normalizedScreenSpaceUV = 0;  // no need for now
                //inputData.shadowMask = 0;    // no need for now

            }

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs vpi = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs vni = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.positionWS = vpi.positionWS;
                o.uv0uv1.xy = v.texcoord0.xy * _ST.xy + _ST.zw;
                half3 viewDirWS = _WorldSpaceCameraPos - vpi.positionWS;        // always perspective solution
                o.normalWS = half4(vni.normalWS, viewDirWS.x);
                o.tangentWS = half4(vni.tangentWS, viewDirWS.y);
                o.bitangentWS = half4(vni.bitangentWS, viewDirWS.z);
                o.sh_tangentSign.xyz = SampleSHVertex(o.normalWS.xyz);
                half sign = v.tangentOS.w * unity_WorldTransformParams.w;       // dont' use it on ray-tracing
                o.sh_tangentSign.w = sign;
                o.positionCS = vpi.positionCS;
                #if defined(LIGHTMAP_ON)
                    o.uv0uv1.zw = v.texcoord1 * unity_LightmapST.xy + unity_LightmapST.zw;
                #endif
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                MiniSurfaceData miniSurfaceData;
                InitializeMiniSurfaceData(i.uv0uv1.xy, miniSurfaceData);

                InputData inputData;
                InitializeInputData(i, miniSurfaceData.normalTS, inputData);

                //half ndotv = max(dot(inputData.normalWS, inputData.viewDirectionWS), 0.0);    // I need to fix this
                half ndotv = 0.5h;

                Light light = GetMainLight(inputData.shadowCoord, inputData.positionWS, half4(1, 1, 1, 1));

                inputData.bakedGI *= miniSurfaceData.metalic_occlusion_roughness_emissionMask.g;
                #if defined(LIGHTMAP_ON)
                    inputData.bakedGI = SubtractDirectMainLightFromLightmap(light, inputData.normalWS, inputData.bakedGI);
                #endif

                half3 diffuse;
                half3 specular;
                MiniLightingGeneralWithAdditionalLight(
                    inputData,
                    miniSurfaceData,
                    light,
                    ndotv,
                    diffuse,
                    specular);

                half3 finalColor = (diffuse.rgb + inputData.bakedGI) * miniSurfaceData.albedo + specular.rgb ;
                finalColor += miniSurfaceData.metalic_occlusion_roughness_emissionMask.a * _EmissionColor.rgb;

                #if defined(Debug_Albedo)
                    return half4(miniSurfaceData.albedo, 1);
                #elif defined(Debug_Normal)
                    return half4(inputData.normalWS * 0.5h + 0.5h, 1);
                #elif defined(Debug_Metallic)
                    return half4(miniSurfaceData.metalic_occlusion_roughness_emissionMask.r, miniSurfaceData.metalic_occlusion_roughness_emissionMask.r, miniSurfaceData.metalic_occlusion_roughness_emissionMask.r, 1);
                #elif defined(Debug_AO)
                    return half4(miniSurfaceData.metalic_occlusion_roughness_emissionMask.g, miniSurfaceData.metalic_occlusion_roughness_emissionMask.g, miniSurfaceData.metalic_occlusion_roughness_emissionMask.g, 1);
                #elif defined(Debug_Roughness)
                    return half4(miniSurfaceData.metalic_occlusion_roughness_emissionMask.b, miniSurfaceData.metalic_occlusion_roughness_emissionMask.b, miniSurfaceData.metalic_occlusion_roughness_emissionMask.b, 1);
                #elif defined(Debug_Emission)
                    return half4(miniSurfaceData.metalic_occlusion_roughness_emissionMask.a * _EmissionColor.rgb, 1);
                #elif defined(Debug_Light)
                    return half4(light.color, 1);
                #elif defined(Debug_BakedGI)
                    return half4(inputData.bakedGI, 1);
                #endif

                return half4(finalColor, 1.0h);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            //#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "MiniInput.hlsl"
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile _ ENABLE_VS_SKINNING

            half3 _LightDirection;
            float3 _LightPosition;
            float4 _ShadowBias; // x: depth bias, y: normal bias

            struct Attributes
            {
                float4 positionOS : POSITION;
                half3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float3 ApplyShadowBias(float3 positionWS, half3 normalWS, half3 lightDirection)
            {
                half invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
                float scale = invNdotL * _ShadowBias.y;

                // normal bias is negative since we want to apply an inset normal offset
                positionWS = lightDirection * _ShadowBias.xxx + positionWS;
                positionWS = normalWS * scale.xxx + positionWS;
                return positionWS;
            }

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                half3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    half3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    half3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On   // must output z-value
            ColorMask R // one channel output

            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            #pragma multi_compile _ ENABLE_VS_SKINNING
            #include "MiniInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half frag(Varyings input) : SV_TARGET
            {
                return input.positionCS.z;
            }

            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            // -------------------------------------
            // Render State Commands
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #include "MiniInput.hlsl"

            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MAREMap);        SAMPLER(sampler_MAREMap);

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"


            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionCS = UnityMetaVertexPosition(v.positionOS.xyz, v.uv1, v.uv2);
                o.uv = TRANSFORM_TEX(v.uv0, _BaseMap);
                return o;
            }
            half4 frag(Varyings i) : SV_TARGET
            {
                float2 uv = i.uv;
                MetaInput metaInput;
                metaInput.Albedo = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
                metaInput.Emission = _EmissionColor.rgb * SAMPLE_TEXTURE2D(_MAREMap, sampler_MAREMap, uv).a;

                return UniversalFragmentMeta(i, metaInput);
            }
            ENDHLSL
        }
    }

    Fallback  "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "MiniLitShaderGUI"
}