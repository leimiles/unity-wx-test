Shader "VFX/Simple"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("剔除", float) = 0
        [Enum(R, 0, Alpha, 1)] _MainChannel ("主纹理通道", float) = 0   
        [HDR]_Color ("Color", Color) = (1,1,1,1)
        [HideInInspector]_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestComp ("ZTestComp", float) = 4
        [Toggle] _ZWrite ("深度写入开启", Float) = 0
        [Toggle] _UVFlowEnabled ("UV流动开启", Float) = 0
        _UVSpeedX ("UV Speed X", Float) = 0
        _UVSpeedY ("UV Speed Y", Float) = 0
        
        [Toggle(SOFT_PARTICLE)] _SoftParticle ("软粒子", float) = 0
        _SoftParticleFade ("软粒子衰减系数", Range(0.01, 10)) = 1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "IgnoreProjector" = "True" 
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100

        Pass
        {
            Name "EffectPass"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest [_ZTestComp]
            Cull [_Cull]
            
            HLSLPROGRAM
            #pragma target 2.0
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma shader_feature_local __ SOFT_PARTICLE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 positionSS : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            #define COMPUTE_EYEDEPTH(o) o = -mul(UNITY_MATRIX_MV, input.positionOS).z
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Cutoff;
                half _UVSpeedX;
                half _UVSpeedY;
                half _UVFlowEnabled;
                half _MainChannel;
                half _SoftParticleFade;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output= (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                #if defined(SOFT_PARTICLE)
                    output.positionSS = vertexInput.positionNDC;
                    COMPUTE_EYEDEPTH(output.positionSS.z);
                #endif
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
             
                float2 uv = input.uv;
                if (_UVFlowEnabled > 0.5)
                {
                    uv.x += _Time.y * _UVSpeedX;
                    uv.y += _Time.y * _UVSpeedY;
                }
                
                half4 maintex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half textureAlpha  = lerp(maintex.r, maintex.a, _MainChannel);
                half alpha = textureAlpha * _Color.a * input.color.a;
                half3 col = maintex.rgb * _Color.rgb * input.color.rgb;
                #if defined(SOFT_PARTICLE)
                    float2 screenUV = input.positionSS.xy / input.positionSS.w;
                    float rawDepth = SampleSceneDepth(screenUV);
                    float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float dep_diff = linearDepth - input.positionSS.z;
                    float softParticleAlpha = saturate(smoothstep(0, 1, dep_diff / _SoftParticleFade));
                    alpha *= softParticleAlpha;
                #endif
                
                col *= alpha; 
                return half4(col.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
