Shader "Dino/Character/CharacterLit"
{
    Properties
    {
        /////////////////
        // Input       //
        /////////////////
        [Header(Albedo)]
        [Space(5)]
        [MainColor] _AlbedoColor ("Color", Color) = (1,1,1,1)
        [NoScaleOffset][MainTexture] _AlbedoMap ("AlbedoMap", 2D) = "white" {}
        [Header(Metallic)]
        [Space(5)]
        [NoScaleOffset]_MetallicMap ("MetallicMap", 2D) = "white" {}
        _Metallic ("Metallic", Range(0, 1)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        [Header(Normal)]
        [Space(5)]
        _NormalMapIntensity ("Intensity", Range(0, 10)) = 1
        [NoScaleOffset][Normal] _NormalMap ("NormalMap", 2D) = "bump" {}
        [Header(Emission)]
        [Space(5)]
        _EmissionColor ("Color", Color) = (0, 0, 0, 1)
        [NoScaleOffset]_EmissionMap ("EmissionMap", 2D) = "black" {}
        [Space(10)]
        _ST ("Scale And Offset", Vector) = (1, 1, 0, 0)
        [Header(Matcap)]
        [Space(5)]
        [NoScaleOffset]_Matcap("Matcap", 2D) = "black" {}
        _MatcapIntensity("MatcapIntensity", Range(0, 1)) = 1.0

        /////////////////
        // Stylize     //
        /////////////////
        //Gooch
        _GoochBrightColor ("GoochBrightColor", Color) = (1, 1, 1, 1)
        _GoochDarkColor ("GoochDarkColor", Color) = (0, 0, 0, 1)

        //Rim
        [Toggle]_RimOn ("Rim On", Float) = 1.0
        _RimColor ("RimColor", Color) = (0, 0, 0, 1)
        _RimSize ("RimSize", Range(0.0, 5.0)) = 1.0

        //Iridescence
        [Toggle]_IridescenceOn ("Iridescence On", Float) = 1.0
        _IridescenceColor ("IridescenceColor", Color) = (0, 0, 0, 0.5)
        _IridescenceSize ("IridescenceSize", Range(0.0, 5.0)) = 1.0

        // Occlusion
        _OccludeColor ("OccludeColor", Color) = (0.75, 0.75, 0.75, 1)

        // ShadowAttenuation
        _ShadowAttenuationIntensity ("ShadowAttenuationIntensity", Range(-1.0, 1.0)) = -1
        _ShadowThreshold("ShadowThreshold", Range(0.0, 1.0)) = 0.3
        _ShadowSmooth("ShadowSmooth", Range(0.0, 1.0)) = 0.1

        // Blending state
        _Surface("__surface", Float) = 0.0
        _Blend("__blend", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 0.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0

        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0

        [HideInInspector] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _BaseMap("Base Map (RGB) Smoothness / Alpha (A)", 2D) = "white" {}

        [HideInInspector]_SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 0.5)
        [HideInInspector]_SmoothnessSource("Smoothness Source", Float) = 0.0
        [HideInInspector]_SpecularHighlights("Specular Highlights", Float) = 1.0

        [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector]_QueueControl("_QueueControl", Float) = -1

        _TargetWorldPosition("Target World Position", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry-10" "RenderType" = "Opaque" "IgnoreProjector" = "True"
        }
        Pass
        {
            Name "UniversalForward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            // -------------------------------------
            // Render State Commands
            // Use same blending / depth states as Standard shader
            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            ZWrite[_ZWrite]
            Cull[_Cull]
            AlphaToMask[_AlphaToMask]

            HLSLPROGRAM
            #pragma target 2.0
            #include "Dino_CommonFunction.hlsl"
            #include "Dino_Character_Input.hlsl"
            #include "Dino_Character_Lighting.hlsl"


            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma shader_feature_local _ _ADDITIONAL_LIGHTS

            #pragma multi_compile_fragment _ _SHADOWS_SOFT
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
                half4 vertexColor : COLOR0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 uv0uv1 : TEXCOORD0;
                half4 normalWS : TEXCOORD2; // w = viewDir.x
                half4 tangentWS : TEXCOORD3; // w = viewDir.y
                half4 bitangentWS : TEXCOORD4; // w = viewDir.z
                half4 sh_tangentSign : TEXCOORD5;
                float3 positionWS : TEXCOORD6;
                half3 viewPos : TEXCOORD7;
                half4 vertexColor : TEXCOORD8;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
            {
                return half4(SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv));
            }

            inline void InitializeMiniSurfaceData(half2 uv, half3 viewPos, half3 normalWS,
                                                  out MiniSurfaceData outMiniSurfaceData)
            {
                outMiniSurfaceData = (MiniSurfaceData)0;
                half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_AlbedoMap, sampler_AlbedoMap));
                outMiniSurfaceData.albedo = albedoAlpha.rgb * _AlbedoColor.rgb;
                outMiniSurfaceData.alpha = albedoAlpha.a * _AlbedoColor.a;
                outMiniSurfaceData.normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv));

                half4 metalic_smoothness = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, uv);
                metalic_smoothness.rgb *= _Metallic;
                metalic_smoothness.a *= _Smoothness;
                outMiniSurfaceData.metalic_occlusion_roughness_emissionMask = half4(
                    metalic_smoothness.r, 1.0, metalic_smoothness.a, 0.0);

                // _MAREConfig = saturate(_MAREConfig);
                // outMiniSurfaceData.metalic_occlusion_roughness_emissionMask = LinearToSRGB(outMiniSurfaceData.metalic_occlusion_roughness_emissionMask);
                outMiniSurfaceData.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb *
                    _EmissionColor.rgb;

                half3 viewNorm = mul((half3x3)UNITY_MATRIX_V, normalWS);
                half3 viewDir = normalize(max(viewPos, 0.0001f));
                half3 viewCross = cross(viewDir, viewNorm);
                viewNorm = half3(-viewCross.y, viewCross.x, 0.0);
                half2 matcapUV = (viewNorm * 0.495 + 0.5).xy;
                outMiniSurfaceData.matcapUV = matcapUV;
            }

            void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
            {
                inputData = (InputData)0;
                inputData.positionWS = input.positionWS; // no need for now
                half3 viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
                half sign = input.sh_tangentSign.w;
                input.bitangentWS.xyz = sign * (cross(input.normalWS.xyz, input.tangentWS.xyz));
                inputData.tangentToWorld = half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
                inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
                inputData.viewDirectionWS = SafeNormalize(viewDirWS); // do it in vertex stage

                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                // just because we only need shadow cascade situation

                //inputData.fogCoord = 0; //    no need for now
                //inputData.vertexLighting = 0    // no need for now
                #if defined(LIGHTMAP_ON)
                    inputData.bakedGI = SampleLightmap(input.uv0uv1.zw, 0, inputData.normalWS);
                #else
                inputData.bakedGI = SampleSHPixel(input.sh_tangentSign.xyz, inputData.normalWS);
                #endif
                //inputData.bakedGI = LinearToSRGB(inputData.bakedGI);        // only for minirp
                //inputData.normalizedScreenSpaceUV = 0;  // no need for now
                //inputData.shadowMask = 0;    // no need for now
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                VertexPositionInputs vpi = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs vni = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionWS = vpi.positionWS;
                output.uv0uv1.xy = input.texcoord0.xy * _ST.xy + _ST.zw;
                half3 viewDirWS = _WorldSpaceCameraPos - vpi.positionWS; // always perspective solution
                output.normalWS = half4(vni.normalWS, viewDirWS.x);
                output.tangentWS = half4(vni.tangentWS, viewDirWS.y);
                output.bitangentWS = half4(vni.bitangentWS, viewDirWS.z);
                output.sh_tangentSign.xyz = SampleSHVertex(output.normalWS.xyz);
                half sign = input.tangentOS.w * unity_WorldTransformParams.w; // dont' use it on ray-tracing
                output.sh_tangentSign.w = sign;
                output.positionCS = vpi.positionCS;
                #if defined(LIGHTMAP_ON)
                    output.uv0uv1.zw = input.texcoord1 * unity_LightmapST.xy + unity_LightmapST.zw;
                #endif
                output.viewPos = -mul(UNITY_MATRIX_MV, input.positionOS).xyz;
                output.vertexColor = input.vertexColor;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                

                MiniSurfaceData miniSurfaceData;
                InitializeMiniSurfaceData(input.uv0uv1.xy, input.viewPos, input.normalWS.xyz, miniSurfaceData);

                InputData inputData;
                InitializeInputData(input, miniSurfaceData.normalTS, inputData);

                //half ndotv = max(dot(inputData.normalWS, inputData.viewDirectionWS), 0.0);    // I need to fix this
                half ndotv = 0.5h;

                Light light = GetMainLight(inputData.shadowCoord, inputData.positionWS, half4(1, 1, 1, 1));


                // inputData.bakedGI *= miniSurfaceData.metalic_occlusion_roughness_emissionMask.g;
                #if defined(LIGHTMAP_ON)
                    inputData.bakedGI = SubtractDirectMainLightFromLightmap(light, inputData.normalWS, inputData.bakedGI);
                #endif

                BRDFData brdfData;
                half brdfAlpha = 1.0;
                InitializeBRDFData(miniSurfaceData.albedo, miniSurfaceData.metalic_occlusion_roughness_emissionMask.r,
                            half3(0.0, 0.0, 0.0),
                            miniSurfaceData.metalic_occlusion_roughness_emissionMask.b, brdfAlpha, brdfData);
                half3 giColor = MiniGlobalIllumination(brdfData, inputData.bakedGI,
           miniSurfaceData.metalic_occlusion_roughness_emissionMask.g,
           inputData.normalWS, inputData.viewDirectionWS,
           TEXTURE2D_ARGS(_Matcap, sampler_Matcap), miniSurfaceData.matcapUV, _MatcapIntensity);
                // BRDFData brdfDataClearCoat = (BRDFData)0;
                // half3 giColor = GlobalIllumination(brdfData, brdfDataClearCoat, 0, inputData.bakedGI,
                //                                    miniSurfaceData.metalic_occlusion_roughness_emissionMask.g, inputData.positionWS,
                //                                    inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);

                half3 diffuse;
                half3 specular;
                half shadowIntensity = lerp(1, -_ShadowAttenuationIntensity, input.vertexColor.r);
                MiniLightingGeneralWithAdditionalLight(brdfData, inputData, light, ndotv, _GoochDarkColor.rgb,
                                                                              _GoochBrightColor.rgb, shadowIntensity,
                                                                              _ShadowThreshold, _ShadowSmooth, diffuse,
                                                                              specular);
                // diffuse = lerp(surface.goochDark.rgb, surface.goochBright.rgb, max(diffuse.r, max(diffuse.g, diffuse.b)));

                // half3 finalColor = (diffuse.rgb + inputData.bakedGI) * miniSurfaceData.albedo + specular.rgb ;


                // half ndotl = max(dot(inputData.normalWS, light.direction), 0.0h);
                // half3 diffuse = ndotl * light.shadowAttenuation;
                // diffuse = LinearStep( _ShadowThreshold - _ShadowSmooth, _ShadowThreshold + _ShadowSmooth, diffuse);
                // diffuse = lerp(diffuse * 0.5 + 0.5, 1, _ShadowAttenuationIntensity);
                //
                // return half4(diffuse * brdfData.diffuse, 1.0h);


                half3 finalColor = diffuse.rgb * brdfData.diffuse + specular.rgb + giColor;
                finalColor += miniSurfaceData.emission;

                // iridescence
                half4 iridescence;
                half oneMinusVoN = saturate(1.0 - dot(inputData.viewDirectionWS, inputData.normalWS));
                iridescence.a = pow(oneMinusVoN, _IridescenceSize);
                iridescence.rgb = _IridescenceColor.rgb;
                finalColor = lerp(finalColor, iridescence.rgb, iridescence.a * _IridescenceColor.a * _IridescenceOn);

                half4 rim;
                rim.a = pow(oneMinusVoN, _RimSize);
                rim.rgb = rim.a;
                finalColor = lerp(finalColor, _RimColor.rgb, rim.rgb * _RimColor.a * _RimOn);

                half alpha = OutputAlpha(miniSurfaceData.alpha, IsSurfaceTypeTransparent(_Surface));
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "CharacterOcc"
            Tags
            {
                "LightMode" = "CharacterOcc"
            }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            #include "Dino_Character_Input.hlsl"

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

            half4 frag(Varyings input) : SV_TARGET
            {
                return _OccludeColor;
            }
            ENDHLSL
        }
        

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
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
            #include "Dino_Character_Input.hlsl"
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

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
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On // must output z-value
            ColorMask R // one channel output

            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            #include "Dino_Character_Input.hlsl"

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
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.CharacterGUI"
}