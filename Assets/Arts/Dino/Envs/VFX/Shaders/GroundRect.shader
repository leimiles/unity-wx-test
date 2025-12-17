Shader "Dino/Mini/GroundRect"
{
    Properties
    {
        _GridColor ("线的颜色", Color) = (1,1,1,1)
        _LineWidth ("线宽(世界单位)", Float) = 0.02
        [HideInInspector]_AASmoothing ("抗锯齿值", Range(0.001, 0.1)) = 0.01
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
        Cull Back

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
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 posWS2 : TEXCOORD0;      // 顶点在世界单位(相对物体中心)的XZ
                float2 halfWS : TEXCOORD1;      // 平面在世界单位的半尺寸(XZ)
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _GridColor;
                float _LineWidth;
                float _AASmoothing;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(input.positionOS.xyz);

                // 从对象到世界矩阵列向量长度计算缩放
                float3 worldScale = float3(
                    length(UNITY_MATRIX_M._m00_m10_m20),
                    length(UNITY_MATRIX_M._m01_m11_m21),
                    length(UNITY_MATRIX_M._m02_m12_m22)
                );

                // Unity 内置 Plane 的半边长是 5（对象空间）
                const float planeHalf = 5.0;
                o.halfWS = float2(planeHalf * worldScale.x, planeHalf * worldScale.z);
                // 顶点在世界单位下的相对中心位置（仅XZ）
                float3 posWSRelative = input.positionOS.xyz * worldScale;
                o.posWS2 = posWSRelative.xz;
                return o;
            }

            // 圆角矩形 SDF（世界单位）
            // p: 位置（世界单位，中心为0），halfExtents: 半尺寸(XZ)，radius: 圆角半径
            float sdRoundedRect(float2 p, float2 halfExtents, float radius)
            {
                float2 b = max(halfExtents - radius, 0.0);
                float2 d = abs(p) - b;
                // 外部距离 + 内部最大分量，最后减去半径
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - radius;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // 线宽使用全宽；阈值采用半宽
                float halfWidth = max(_LineWidth * 0.5, 1e-5);
                float aa = _AASmoothing;

                // 自适配的圆角半径：与线宽相关，并限制不超过最短半边
                float minHalf = min(i.halfWS.x, i.halfWS.y);
                float radius = clamp(_LineWidth * 2.0, 0.0, max(minHalf - halfWidth, 0.0));

                // 到圆角矩形边界的符号距离（世界单位）
                float d = sdRoundedRect(i.posWS2, i.halfWS, radius);
                float ad = abs(d);

                // 仅渲染边框：|d| <= halfWidth，使用平滑过渡抗锯齿
                float alpha = 1.0 - smoothstep(halfWidth - aa, halfWidth + aa, ad);

                return half4(_GridColor.rgb, _GridColor.a * alpha);
            }
            ENDHLSL
        }
    }
}
