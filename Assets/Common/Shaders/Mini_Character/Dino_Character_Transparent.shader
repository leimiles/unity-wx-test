Shader "Dino/Character/Transparent"
{
    Properties
    {
        [MainTexture] _AlbedoMap("Base Map (RGB) Smoothness / Alpha (A)", 2D) = "white" {}
        [MainColor] _AlbedoColor("Base Color", Color) = (1, 1, 1, 1)

        _Cutoff("Alpha Clipping", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _SmoothnessSource("Smoothness Source", Float) = 0.0
        _SpecularHighlights("Specular Highlights", Float) = 1.0

        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        [NoScaleOffset]_EmissionMap("Emission Map", 2D) = "white" {}

        [Header(Gooch)]
        _GoochBrightColor ("GoochBrightColor", Color) = (1, 1, 1, 1)
        _GoochDarkColor ("GoochDarkColor", Color) = (0, 0, 0, 1)

        // Blending state
        _Surface("__surface", Float) = 1.0
        _Blend("__blend", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
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
        
        [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector]_QueueControl("_QueueControl", Float) = -1
        

        _TargetWorldPosition("Target World Position", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "SimpleLit"
            "IgnoreProjector" = "True"
            "Queue" = "Transparent-200"
        }

        Pass
        {
            Name "ForwardLit"
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
            #include "Dino_Character_Transparent_Input.hlsl"
            #include "Dino_Character_Lighting.hlsl"


            // -------------------------------------
            // Shader Stages
            #pragma vertex vert
            #pragma fragment frag
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #define _SURFACE_TYPE_TRANSPARENT 1
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #pragma multi_compile _ _LIGHTMAP_ON
            #pragma multi_compile _ _DIRLIGHTMAP_COMBINED

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
                half4 normalWS : TEXCOORD2; // w = viewDir.x
                half4 tangentWS : TEXCOORD3; // w = viewDir.y
                half4 bitangentWS : TEXCOORD4; // w = viewDir.z
                half4 sh_tangentSign : TEXCOORD5;
                float3 positionWS : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // half FilterDither(half4 positionCS, half threshold, half DitherScale)
            // {
            //     half2 uv = positionCS.xy * DitherScale;
            //     half d = SAMPLE_TEXTURE2D(_DitheringTexture, sampler_PointRepeat, uv).a;
            //     return threshold - d;
            // }

            void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
            {
                inputData = (InputData)0;
                inputData.positionWS = input.positionWS; // no need for now
                half3 viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
                // half sign = input.sh_tangentSign.w;
                // input.bitangentWS.xyz = sign * (cross(input.normalWS.xyz, input.tangentWS.xyz));
                // inputData.tangentToWorld = half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
                // inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
                // inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
                // inputData.viewDirectionWS = SafeNormalize(viewDirWS); // do it in vertex stage
                inputData.normalWS = input.normalWS.xyz;
                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
                viewDirWS = SafeNormalize(viewDirWS);
                inputData.viewDirectionWS = viewDirWS;

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

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs vpi = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs vni = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                o.positionWS = vpi.positionWS;
                o.uv0uv1.xy = TRANSFORM_TEX(v.texcoord0.xy, _AlbedoMap);
                half3 viewDirWS = _WorldSpaceCameraPos - vpi.positionWS; // always perspective solution
                o.normalWS = half4(vni.normalWS, viewDirWS.x);
                o.tangentWS = half4(vni.tangentWS, viewDirWS.y);
                o.bitangentWS = half4(vni.bitangentWS, viewDirWS.z);
                o.sh_tangentSign.xyz = SampleSHVertex(o.normalWS.xyz);
                half sign = v.tangentOS.w * unity_WorldTransformParams.w; // dont' use it on ray-tracing
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
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                
                SurfaceData surfaceData;
                InitializeSimpleLitSurfaceData(i.uv0uv1.xy, surfaceData);

                InputData inputData;
                InitializeInputData(i, surfaceData.normalTS, inputData);
                SETUP_DEBUG_TEXTURE_DATA(inputData, i.uv, _AlbedoMap);

                half4 color = CharacterBlinnPhong(inputData, surfaceData, _GoochDarkColor.rgb, _GoochBrightColor.rgb);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));
                
                return color;
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

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // -------------------------------------
            // Includes
            #include "Dino_Character_Transparent_Input.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            // -------------------------------------
            // Render State Commands
            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0

            // -------------------------------------
            // Shader Stages
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _GLOSSINESS_FROM_BASE_ALPHA

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Dino_Character_Transparent_Input.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.CharacterTransparentGUI"
}