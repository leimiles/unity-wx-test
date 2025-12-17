Shader "Dino/ArrowGuideShader"
{
    Properties
    {
        _MainTex ("Arrow Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _ScrollSpeed ("Scroll Speed", Float) = 1.0
        _FadeLength ("Fade Length", Float) = 1.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float pathPosition : TEXCOORD1;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _ScrollSpeed;
                float _FadeLength;
            CBUFFER_END
            
            Varyings vert (Attributes input)
            {
                Varyings output;
                
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.pathPosition = input.uv.x;
                
                return output;
            }
            
            half4 frag (Varyings input) : SV_Target
            {
                float2 flowUV = input.uv;
                flowUV.x -= _Time.y * _ScrollSpeed * 0.1;
                
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, flowUV);
                col *= _Color;
                
                float fadeAlpha = 1.0;
                if (input.pathPosition < 0.1) 
                    fadeAlpha = smoothstep(0.0, 0.1, input.pathPosition);
                else if (input.pathPosition > 0.9) 
                    fadeAlpha = smoothstep(1.0, 0.9, input.pathPosition);
                
                col.a *= fadeAlpha;
                return col;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Unlit"
}
