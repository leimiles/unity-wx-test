Shader "VFX/FrameOnce"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("SrcBlend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("DstBlend", Float) = 10
        [Enum(Off, 0, On, 1)] _ZWrite ("深度写入", float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestComp ("ZTestComp", float) = 4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("剔除", float) = 0
        _MainTex ("Main Texture", 2D) = "white" { }
        [Enum(R, 0, Alpha, 1)] _MainChannel ("主纹理通道", float) = 0
        _Columns ("Columns", int) = 4
        _Rows ("Rows", int) = 4
        _FrameIndex ("Frame Index (0 to 1)", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 200

        // Additive blending
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        ZTest [_ZTestComp]
        Cull [_Cull]
        ZTest Always
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 position : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _MainTex_ST;
                uint _Columns, _Rows;
                float _FrameIndex; // Updated to be a float ranging from 0 to 1
                half _MainChannel;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.position = TransformObjectToHClip(input.vertex);
                //output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.uv = input.uv;
                return output;
            }

            float2 CalculateFrameOffset(uint frame, uint columns, uint rows)
            {
                uint column = frame % columns;
                uint row = rows - 1 - (frame / columns); // Adjust row calculation to account for UV flipping
                return float2(column / (float)columns, row / (float)rows);
            }

            float2 CalculateFrameScale(uint columns, uint rows)
            {
                return float2(1.0f / columns, 1.0f / rows);
            }

            half4 frag(Varyings input) : SV_Target
            {
                //return half4(0, 1, 0, 1);
                uint totalFrames = _Columns * _Rows;

                // Calculate current frame index based on _FrameIndex range 0 to 1
                uint frame = min(max(0, (uint) (floor(_FrameIndex * totalFrames))), totalFrames - 1);

                float2 scale = CalculateFrameScale(_Columns, _Rows);
                float2 offset = CalculateFrameOffset(frame, _Columns, _Rows);

                float2 uv = input.uv * scale + offset;
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half3 finalColor = lerp(texColor.rgb, texColor.a, _MainChannel);
                half alpha = texColor.a;

                return float4(finalColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Unlit/Transparent"
}
