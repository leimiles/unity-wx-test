Shader "Dino/Mini/MiniLit_Transparent02"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _BaseMap("Base Map", 2D) = "white" {}
        [Enum(UV0,0, UV1,1, UV2,2, UV3,3, UV4,4)] _UVSet ("UV Set", Float) = 1
        
        _AmbientIntensity("Ambient Intensity", Range(0.0, 5.0)) = 3.5
        _Alpha("Alpha", Range(0.0, 1.0)) = 0.5
        
    }

    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }
        
        Pass
        {
            ZWrite On
            ColorMask 0
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
                float2 uv4 : TEXCOORD4;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _AmbientIntensity;
                float _Alpha;
                float _UVSet;
            CBUFFER_END


            half2 SelectUV(Attributes input)
            {
                if (_UVSet == 0) return input.uv;
                if (_UVSet == 1) return input.uv1;
                if (_UVSet == 2) return input.uv2;
                if (_UVSet == 3) return input.uv3;
                if (_UVSet == 4) return input.uv4;
                return input.uv;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                
                half2 selectedUV = SelectUV(IN);
                OUT.uv = TRANSFORM_TEX(selectedUV, _BaseMap);
                
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                float finalAlpha = color.a * _Alpha;

                half3 ambientColor = SampleSH(IN.normalWS);
                color.rgb *= ambientColor * _AmbientIntensity;
                
                color.a = finalAlpha;
                
                return color;
            }
            ENDHLSL
        }
    }
}
