Shader "Dino/Mini/MiniLit"
{
    Properties
    {
        // override properties, this shader don't support alphaclip
        [Enum(Zero, 0, Occ, 3)]_Stencil ("Stencil ID", float) = 3
        _StencilZFail ("Stencil ZFail", Int) = 0
        
        [Enum(UV0,0, UV1,1, UV2,2, UV3,3, UV4,4)] _UVSet ("UV Set", Float) = 0
        
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset]_BaseMap ("Base Map", 2D) = "white" { }
        [NoScaleOffset]_BumpMap ("Normal Map", 2D) = "bump" { }
        [NoScaleOffset]_MetallicGlossMap ("MAR Map", 2D) = "white" { }
        [NoScaleOffset]_EmissionMap ("Emission", 2D) = "white" { }
        [HDR]_EmissionColor ("Emission Color", Color) = (0, 0, 0, 0)

        [Space(10)]
        [NoScaleOffset] _MAREConfig ("MARE Configure", Vector) = (1, 1, 1, 1)
        _ST ("Scale And Offset", Vector) = (1, 1, 0, 0)
        [NoScaleOffset]_Matcap ("Mat Cap", 2D) = "black" { }
        _MatcapIntensity ("MatcapIntensity", float) = 1
        [Toggle(_ADDITIONAL_LIGHTS)] _AddLightOn ("Additional Light On", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        
        Pass
        {
            Name "UniversalForward"
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
            #pragma target 2.0
            #include "Dino_MiniInput.hlsl"
            #include "Dino_MiniLighting.hlsl"

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma shader_feature_local _ _ADDITIONAL_LIGHTS
            // #pragma shader_feature _ Debug_Albedo Debug_Normal Debug_Metallic Debug_AO Debug_Roughness Debug_Emission Debug_Light Debug_BakedGI

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
                half2 texcoord2 : TEXCOORD2;
                half2 texcoord3 : TEXCOORD3;
                half2 texcoord4 : TEXCOORD4;
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            half2 SelectUV(Attributes input)
            {
                if (_UVSet == 0) return input.texcoord0;
                if (_UVSet == 1) return input.texcoord1;
                if (_UVSet == 2) return input.texcoord2;
                if (_UVSet == 3) return input.texcoord3;
                if (_UVSet == 4) return input.texcoord4;
                return input.texcoord0;
            }
            
            inline void InitializeMiniSurfaceData(half2 uv, half3 viewPos, half3 normalWS,
            out MiniSurfaceData outMiniSurfaceData)
            {
                outMiniSurfaceData = (MiniSurfaceData)0;
                outMiniSurfaceData.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * _BaseColor.rgb;
                outMiniSurfaceData.normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv));
                outMiniSurfaceData.metalic_occlusion_roughness_emissionMask = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);

                _MAREConfig = saturate(_MAREConfig);
                outMiniSurfaceData.metalic_occlusion_roughness_emissionMask = LinearToSRGB(outMiniSurfaceData.metalic_occlusion_roughness_emissionMask * _MAREConfig);
                outMiniSurfaceData.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb * _EmissionColor.rgb * _MAREConfig.w;

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

                half2 selectedUV = SelectUV(input);
                output.uv0uv1.xy = selectedUV * _ST.xy + _ST.zw;
                
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
                InitializeBRDFData(miniSurfaceData.albedo, miniSurfaceData.metalic_occlusion_roughness_emissionMask.r, half3(0.0, 0.0, 0.0),
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
                MiniLightingGeneralWithAdditionalLight(brdfData, inputData, light, ndotv, diffuse, specular);



                // half3 finalColor = (diffuse.rgb + inputData.bakedGI) * miniSurfaceData.albedo + specular.rgb ;
                half3 finalColor = diffuse.rgb * miniSurfaceData.albedo + specular.rgb + giColor;
                finalColor += miniSurfaceData.emission;

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

                // return half4(miniSurfaceData.metalic_occlusion_roughness_emissionMask.b, miniSurfaceData.metalic_occlusion_roughness_emissionMask.b, miniSurfaceData.metalic_occlusion_roughness_emissionMask.b, 1);

                
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
            #include "Dino_MiniInput.hlsl"
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
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On // must output z-value
            ColorMask R // one channel output

            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            #include "Dino_MiniInput.hlsl"

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
            #include "Dino_MiniInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"


            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv2);
                output.uv = TRANSFORM_TEX(input.uv0, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_TARGET
            {
                float2 uv = input.uv;
                MetaInput metaInput;
                metaInput.Albedo = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
                metaInput.Emission = _EmissionColor.rgb * SAMPLE_TEXTURE2D(
                    _MetallicGlossMap, sampler_MetallicGlossMap, uv).a;

                    return UniversalFragmentMeta(input, metaInput);
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
    //    CustomEditor "MiniLitShaderGUI"

}