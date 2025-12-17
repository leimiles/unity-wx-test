Shader "Dino/Mini/VariationLit"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2
        [Toggle(_ALPHATEST_ON)] _AlphaTest("开启透明裁剪",Float) = 0
        _Cutoff("Alpha Clip", Range(0.0, 1.0)) = 0.0
        _BaseColor("漫反射颜色", Color) = (1,1,1,1)

        [NoScaleOffset]_BaseMap("漫反射贴图", 2D) = "black" {}
        [NoScaleOffset]_NormalTex("法线贴图",2D) = "bump"{}
        _NormalScale("NormalScale（法线贴图强度）", Float) = 1
        [NoScaleOffset]_MetallicGlossMap("MAR图(R:Metallic  G:AO   B:roughness)", 2D) = "black"{}
        _Smoothness("光滑度", Range(-0.7,1)) = 0
        _Metallic("金属度", Range(-0.5,1)) = 0
        _Occlusion("环境光遮蔽强度", Range(0,2)) = 1
        _EmissionTex("自发光贴图", 2D) = "white" {}
        _EmissionColor("Emission",Color) = (0,0,0,1)
        
        [Space(10)]
        [Header(Matcap)]
        [Toggle(_MATCAP_ON)] _MapcapOn ("Enable Matcap", Float) = 0
        _Matcap ("MatCap", 2D) = "white"{}
        _MatcapIntensity ("Matcap Intensity", Float) = 1.0
        _MatcapRotationSpeed ("Rotation Speed", Float) = 30.0
        _MatcapAddTex ("Matcap Add Tex", 2D) = "white"{}
        _MatcapAddTexIntensity ("Matcap Add Tex Intensity", Float) = 1.0
        _MatcapAddTexRotationSpeed ("Add Tex Rotation Speed", Float) = -15.0
        
        [Space(10)]
        [Header(Fresnel)]
        [Toggle(_FRESNEL_ON)] _FresnelOn ("Enable Fresnel", Float) = 0
        [HDR]_FresnelColor ("Fresnel Color", Color) = (1,1,1,1)
        _FresnelPower ("Fresnel Power", Range(0, 10)) = 2
        _FresnelBias ("Fresnel Bias", Range(0, 1)) = 0.1
        _BreathSpeed ("Breath Speed", Range(0, 10)) = 1
        _BreathIntensity ("Breath Intensity", Range(0, 1)) = 0.5
        
        [Space(10)]
        [Header(Flow UV)]
        [Toggle(_FLOWUV_ON)] _FlowUVOn ("Enable Flow UV", Float) = 0
        _FlowTex ("Flow Texture", 2D) = "white" {}
        _FlowSpeed ("Flow Speed (X,Y)", Vector) = (0.1, 0.1, 0, 0)
        _FlowTilingOffset ("Tiling (XY) Offset (ZW)", Vector) = (1, 1, 0, 0)
        
        [Header(Stencil)]
        [Enum(Zero, 0, Occ, 3, Selected, 5)] _Stencil ("Stencil ID", float) = 3
        _StencilComp ("Stencil Comparison", Float) = 8 // Always
        _StencilPass ("Stencil Pass", Float) = 2 // Replace
        _StencilFail ("Stencil Fail", Float) = 0 // Keep
        _StencilZFail ("Stencil ZFail", Float) = 0 // Keep
        
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry-30"}

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            //Lightmap
            #pragma multi_compile _ LIGHTMAP_ON
             //Mainlight受光 阴影相关
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            //额外光照
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #pragma multi_compile_fog 
            #pragma multi_compile_local _ _MATCAP_ON
            #pragma multi_compile_local _ _FRESNEL_ON
            #pragma multi_compile_local _ _FLOWUV_ON
            #pragma shader_feature _ _ALPHATEST_ON

            #include "Assets/Arts/Envs/StageGardenBundle/VariationPack/Shaders/Dino_VariationInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct a2v
            {
                float4 vertex : POSITION;
                float4 uv : TEXCOORD0;
                float2 lightmapUV   : TEXCOORD1;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD0;
                //有lightMap定义lightMap，有SH定义SH
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float3 worldPos : TEXCOORD5;
                float3 viewPos : TEXCOORD7;
                float3 viewNormal : TEXCOORD8;
                float4 fogFactor : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 RotateUV(float2 uv, float rotation)
            {
                float rad = rotation * PI / 180.0;
                float2 center = float2(0.5, 0.5);
                uv -= center;
                float s = sin(rad);
                float c = cos(rad);
                float2x2 rotMatrix = float2x2(c, -s, s, c);
                uv = mul(uv, rotMatrix);
                uv += center;
                return uv;
            }
            
            v2f vert(a2v v)
            {
                v2f o= (v2f)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                o.pos = vertexInput.positionCS;
                o.uv.xy = TRANSFORM_TEX(v.uv, _BaseMap);
                o.uv.zw = TRANSFORM_TEX(v.uv, _NormalTex);
                o.worldPos = vertexInput.positionWS;
                
                // 计算视图空间位置和法线
                o.viewPos = TransformWorldToView(o.worldPos);
                o.viewNormal = TransformWorldToViewNormal(TransformObjectToWorldNormal(v.normal));

                VertexNormalInputs normalInputs = GetVertexNormalInputs(v.normal, v.tangent);
                o.normalWS = normalInputs.normalWS;
                o.tangentWS = normalInputs.tangentWS;
                o.bitangentWS = normalInputs.bitangentWS;
                OUTPUT_SH(o.normalWS, o.vertexSH);
                
                // o.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                //贴图采样
                half4 mainTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv.xy);
                half4 nrmTex = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, i.uv.xy);
                half4 marTex = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, i.uv.xy);
                half4 emissionTex = SAMPLE_TEXTURE2D(_EmissionTex, sampler_EmissionTex, i.uv.xy);
                
                //法线贴图
                half3 normalTS = UnpackNormalScale(nrmTex, _NormalScale);
                half3 bump = normalize(TransformTangentToWorld(normalTS, half3x3(i.tangentWS, i.bitangentWS, i.normalWS)));
                float4 SHADOW_COORDS = TransformWorldToShadowCoord(i.worldPos);

                half4 shadowMask = unity_ProbesOcclusion; 
                Light light = GetMainLight(SHADOW_COORDS, i.worldPos, shadowMask);

                half3 lightDir = light.direction;
                half3 lightColor = light.color;
                half lightAtten = light.distanceAttenuation;
                half shadowAtten = light.shadowAttenuation; 
                half3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

                half3 bakedGI = SampleSH(bump); 
                half3 occlusion = LerpWhiteTo(marTex.g, _Occlusion);

                // 材质参数计算
                half3 albedo = mainTex.rgb * _BaseColor.rgb;
                half metallic = saturate(marTex.r + _Metallic); 
                half roughness = saturate(1.0 - (_Smoothness + marTex.b));
                half smoothness = 1.0 - roughness;
                half4 emissionColor = emissionTex * _EmissionColor;

                // BRDF初始化
                BRDFData brdf;
                BRDFData brdfDataClearCoat = (BRDFData)0;
                InitializeBRDFData(albedo, metallic, 0.5, smoothness, _BaseColor.a, brdf);

                half3 color = GlobalIllumination(brdf, brdfDataClearCoat,0, bakedGI, occlusion.r, bump, viewDir);
                color += LightingPhysicallyBased(brdf, light, bump, viewDir) * shadowAtten; 
                color += emissionColor.rgb;
                
                //点光源
                #ifdef _ADDITIONAL_LIGHTS
                uint addLightsCount = GetAdditionalLightsCount();//返回一个额外灯光的数量
                for (uint idx = 0; idx < addLightsCount; idx++)
                {
                    //返回一个灯光类型的数据
                    Light addLight = GetAdditionalLight(idx, i.worldPos);
                    color += LightingPhysicallyBased(brdf,brdfDataClearCoat,addLight, bump, viewDir, 0.0, false);
                }
                #endif
                
                #if _MATCAP_ON 
                float matcapRotation = _MatcapRotationSpeed * _Time.y;
                float matcapAddRotation = _MatcapAddTexRotationSpeed * _Time.y;
                half3 normal_viewspace = mul(GetWorldToViewMatrix(), float4(bump, 0.0)).xyz;
                half2 uv_matcap = (normal_viewspace.xy + float2(1.0, 1.0)) * 0.5;
                uv_matcap = RotateUV(uv_matcap, matcapRotation);
                half3 matcap_color = SAMPLE_TEXTURE2D(_Matcap, sampler_Matcap, uv_matcap).rgb * _MatcapIntensity;
                half2 uv_matcap_add = RotateUV(uv_matcap, matcapAddRotation);
                half3 matcap_add_color = SAMPLE_TEXTURE2D(_MatcapAddTex, sampler_MatcapAddTex, uv_matcap_add).rgb * _MatcapAddTexIntensity;
                color += matcap_color + matcap_add_color;
                #endif
                
                #if _FRESNEL_ON
                    float fresnel = saturate(_FresnelBias + (1 - _FresnelBias) * pow(1 - saturate(dot(bump, viewDir)), _FresnelPower));
                    
                    float breathFactor = (sin(_Time.y * _BreathSpeed) * 0.5 + 0.5) * _BreathIntensity + (1 - _BreathIntensity);
                    fresnel *= breathFactor;
                    
                    color += _FresnelColor.rgb * fresnel;
                #endif
                
                #if _FLOWUV_ON
                    float3 viewOrigin = TransformWorldToView(float3(0, 0, 0));
                    float2 relativePos = (i.viewPos.xy - viewOrigin.xy);
                    float2 baseUV = relativePos * _FlowTilingOffset.xy + _FlowTilingOffset.zw;
                    float2 flowUV = baseUV  + _Time.y * _FlowSpeed;
                    half4 flowColor = SAMPLE_TEXTURE2D(_FlowTex, sampler_FlowTex, flowUV);
                    color += flowColor;
                #endif

                // color.rgb = MixFog(color.rgb,i.fogFactor.x);
                
                half Alpha = _BaseColor.a * mainTex.a;

                #ifdef _ALPHATEST_ON
                    clip(Alpha - _Cutoff);
                #endif
                
                return half4 (color,Alpha);
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
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Assets/Arts/Envs/StageGardenBundle/VariationPack/Shaders/Dino_VariationInput.hlsl"
            
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

            #include "Assets/Arts/Envs/StageGardenBundle/VariationPack/Shaders/Dino_VariationInput.hlsl"

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
            Tags
            {
                "LightMode" = "Meta"
            }

            // -------------------------------------
            // Render State Commands
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Assets/Arts/Envs/StageGardenBundle/VariationPack/Shaders/Dino_VariationInput.hlsl"
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
    FallBack "Hidden/InternalErrorShader"
}
