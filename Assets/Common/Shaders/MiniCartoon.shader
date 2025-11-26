Shader "SoFunny/Mini/MiniCartoon"
{
    Properties
    {
        // override properties, this shader don't support alphaclip
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset]_BaseMap ("Base Map", 2D) = "white" { }
        [NoScaleOffset]_NormalMap ("Normal Map", 2D) = "bump" { }
        _ST ("Scale And Offset", Vector) = (1, 1, 0, 0)
        [Header(Toon)]
        [Space(10)]
        [Toggle(_ADDITIONAL_LIGHTS)] _AddLightOn ("Additional Light On", Float) = 0
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularShininess ("Specular Shininess", Float) = 1
        _SpecularGloss ("Specular Gloss", Range(0, 1)) = 1
        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 1)
        _RampThreshold ("Ramp Threshold", Range(0.01, 1)) = 0.5
        _RampSmooth ("Ramp Smooth", Range(0.01, 1)) = 0.5
        [Header(Rim Lighting)]
        [Space(10)]
        [KeywordEnum(TowardsLight, BackLight, RimLight)] _RimTowards ("Rim Towards", Float) = 0
        _RimColor ("Rim Color", Color) = (0.8, 0.8, 0.8, 0.5)
        _RimThreshold ("Rim Threshold", Range(0.01, 1)) = 0.5
        _RimSmooth ("Rim Smooth", Range(0.01, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" "RenderType" = "Opaque" "IgnoreProjector" = "True" }
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #include "MiniCartoonInput.hlsl"
            #include "MiniLighting.hlsl"

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma shader_feature_local _ADDITIONAL_LIGHTS
            #pragma shader_feature_local _RIMTOWARDS_TOWARDSLIGHT _RIMTOWARDS_BACKLIGHT _RIMTOWARDS_RIMLIGHT

            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ ENABLE_VS_SKINNING

            #pragma multi_compile_instancing

            #pragma vertex vert
            #pragma fragment fragment

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
                half2 uv0 : TEXCOORD0;
                half4 normalWS : TEXCOORD2; // w = viewDir.x
                half4 tangentWS : TEXCOORD3; // w = viewDir.y
                half4 bitangentWS : TEXCOORD4; // w = viewDir.z
                half4 sh_tangentSign : TEXCOORD5;
                float3 positionWS : TEXCOORD6;
                float4 shadowCoord : TEXCOORD7;
                float4 shadowCoordBackLight : TEXCOORD8; // 自阴影沿灯光方向偏移，避免背光菲涅尔产生锯齿
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            inline void InitializeMiniCartoonSurfaceData(Varyings i, out MiniCartoonSurfaceData outMiniSurfaceData)
            {
                outMiniSurfaceData = (MiniCartoonSurfaceData)0;
                outMiniSurfaceData.albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv0).rgb * _BaseColor.rgb;
                outMiniSurfaceData.normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv0));
                outMiniSurfaceData.highlightColor = _HighlightColor;
                outMiniSurfaceData.shadowColor = _ShadowColor;
                outMiniSurfaceData.rimColor = _RimColor;
                outMiniSurfaceData.cartoonProps = half4(_RampThreshold, _RampSmooth, _RimThreshold, _RimSmooth);
                outMiniSurfaceData.specular = half4(_SpecularColor.rgb, _SpecularShininess);
                outMiniSurfaceData.specularGloss = _SpecularGloss;

                // 自阴影沿灯光方向偏移，避免背光菲涅尔产生锯齿
                #if defined(_MAIN_LIGHT_SHADOWS)
                    outMiniSurfaceData.shadowCoordBackLight = i.shadowCoordBackLight;
                #else
                    outMiniSurfaceData.shadowCoordBackLight = float4(0, 0, 0, 0);
                #endif
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

                //inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                #if defined(_MAIN_LIGHT_SHADOWS)
                    inputData.shadowCoord = input.shadowCoord;
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif
                // just because we only need shadow cascade situation

                //inputData.fogCoord = 0; //    no need for now
                //inputData.vertexLighting = 0    // no need for now
                inputData.bakedGI = SampleSHPixel(input.sh_tangentSign.xyz, inputData.normalWS);
                inputData.bakedGI = LinearToSRGB(inputData.bakedGI); // only for minirp
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
                o.uv0.xy = v.texcoord0.xy * _ST.xy + _ST.zw;
                half3 viewDirWS = _WorldSpaceCameraPos - vpi.positionWS; // always perspective solution
                o.normalWS = half4(vni.normalWS, viewDirWS.x);
                o.tangentWS = half4(vni.tangentWS, viewDirWS.y);
                o.bitangentWS = half4(vni.bitangentWS, viewDirWS.z);
                o.sh_tangentSign.xyz = SampleSHVertex(o.normalWS.xyz);
                half sign = v.tangentOS.w * unity_WorldTransformParams.w; // dont' use it on ray-tracing
                o.sh_tangentSign.w = sign;
                o.positionCS = vpi.positionCS;
                o.shadowCoord = TransformWorldToShadowCoord(vpi.positionWS);

                Light mainLight = GetMainLight();
                vpi.positionWS.xyz += normalize(mainLight.direction);
                o.shadowCoordBackLight = TransformWorldToShadowCoord(vpi.positionWS);
                return o;
            }

            half4 fragment(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i)

                MiniCartoonSurfaceData miniSurfaceData;
                InitializeMiniCartoonSurfaceData(i, miniSurfaceData);

                InputData inputData;
                InitializeInputData(i, miniSurfaceData.normalTS, inputData);
                Light light = GetMainLight(inputData.shadowCoord, inputData.positionWS, half4(1, 1, 1, 1));

                half3 diffuse;
                half3 specular;
                MiniLightingCartoon(
                    inputData,
                    miniSurfaceData,
                    light,
                    diffuse,
                    specular);

                half3 finalColor = diffuse.rgb + inputData.bakedGI * miniSurfaceData.albedo + specular.rgb;

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
            #include "MiniCartoonInput.hlsl"
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

            ZWrite On // must output z-value
            ColorMask R // one channel output

            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            #pragma multi_compile _ ENABLE_VS_SKINNING
            #include "MiniCartoonInput.hlsl"

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
            #include "MiniCartoonInput.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MAREMap);
            SAMPLER(sampler_MAREMap);

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
                metaInput.Emission = half3(0, 0, 0);

                return UniversalFragmentMeta(i, metaInput);
            }
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}