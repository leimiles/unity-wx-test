Shader "Dino/Mini/Grid"
{
    Properties
    {
        _GridColor ("线的颜色", Color) = (1,1,1,1)
        _SquareSize ("栅格边长(m)", Float) = 1.0
        _GridOffset ("栅格偏移(格)", Float) = 0.5
        _LineWidth ("线宽(m)", Float) = 0.02
        _AASmoothing ("抗锯齿值", Range(0.001, 0.1)) = 0.01
        _MinorLineAlpha ("虚线强度", Range(0.0, 1.0)) = 0.5
        _MinorDashFrequency ("虚线重复频率", Float) = 3.0
        _MinorDashDuty ("虚线占空比", Range(0.05, 0.95)) = 0.35
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
                float3 modelPos : TEXCOORD0;
                float2 objPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _GridColor;
                float _SquareSize;
                float _GridOffset;
                float _LineWidth;
                float _AASmoothing;
                float _MinorLineAlpha;
                float _MinorDashFrequency;
                float _MinorDashDuty;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                
                float3 worldScale = float3(
                    length(UNITY_MATRIX_M._m00_m10_m20),
                    length(UNITY_MATRIX_M._m01_m11_m21),
                    length(UNITY_MATRIX_M._m02_m12_m22)
                );
                
                output.modelPos = input.positionOS.xyz * worldScale;
                output.objPos = input.positionOS.xz;
                
                return output;
            }

            float computeGridLinesWithAA(float3 modelPos)
            {
                float2 gridPos = (modelPos.xz / _SquareSize) + _GridOffset;
                float2 fractional = frac(gridPos);

                float2 distToLine = min(fractional, 1.0 - fractional);
                float lineWidthInGridSpace = _LineWidth / _SquareSize;

                float baseX = smoothstep(lineWidthInGridSpace + _AASmoothing,
                                        lineWidthInGridSpace - _AASmoothing,
                                        distToLine.x);
                float baseZ = smoothstep(lineWidthInGridSpace + _AASmoothing,
                                        lineWidthInGridSpace - _AASmoothing,
                                        distToLine.y);

                float nearestIndexX = floor(gridPos.x + 0.5);
                float parityX = frac(nearestIndexX * 0.5);
                float majorMaskX = 1.0 - step(0.25, parityX);
                float minorMaskX = 1.0 - majorMaskX;

                float nearestIndexZ = floor(gridPos.y + 0.5);
                float parityZ = frac(nearestIndexZ * 0.5);
                float majorMaskZ = 1.0 - step(0.25, parityZ);
                float minorMaskZ = 1.0 - majorMaskZ;

                float dashAA = _AASmoothing * 0.5;
                float dashPhaseX = frac(gridPos.y * _MinorDashFrequency);
                float dashTriX = abs(dashPhaseX - 0.5) * 2.0;
                float dashMaskX = smoothstep(_MinorDashDuty, _MinorDashDuty - dashAA, dashTriX);

                float dashPhaseZ = frac(gridPos.x * _MinorDashFrequency);
                float dashTriZ = abs(dashPhaseZ - 0.5) * 2.0;
                float dashMaskZ = smoothstep(_MinorDashDuty, _MinorDashDuty - dashAA, dashTriZ);

                float minorIntensity = _MinorLineAlpha;
                float majorLineX = baseX * majorMaskX;
                float minorLineX = baseX * minorMaskX * dashMaskX * minorIntensity;
                float combinedX = max(majorLineX, minorLineX);

                float majorLineZ = baseZ * majorMaskZ;
                float minorLineZ = baseZ * minorMaskZ * dashMaskZ * minorIntensity;
                float combinedZ = max(majorLineZ, minorLineZ);

                return max(combinedX, combinedZ);
            }

            float computeBordersWithAA(float2 objPos)
            {
                const float planeHalfSize = 5.0; //Unity自带Plane的半边长
                const float borderWidth = _LineWidth * 2;  //乘2让锁边线的粗细与网格一致
                
                float distToRight = planeHalfSize - objPos.x;
                float distToLeft = planeHalfSize + objPos.x;
                float distToTop = planeHalfSize - objPos.y;
                float distToBottom = planeHalfSize + objPos.y;
                
                float isRightBorder = smoothstep(borderWidth + _AASmoothing, 
                                               borderWidth - _AASmoothing, 
                                               distToRight);
                float isLeftBorder = smoothstep(borderWidth + _AASmoothing,
                                              borderWidth - _AASmoothing,
                                              distToLeft);
                float isTopBorder = smoothstep(borderWidth + _AASmoothing,
                                             borderWidth - _AASmoothing,
                                             distToTop);
                float isBottomBorder = smoothstep(borderWidth + _AASmoothing,
                                                borderWidth - _AASmoothing,
                                                distToBottom);
                
                return max(max(isRightBorder, isLeftBorder), max(isTopBorder, isBottomBorder));
            }

            half4 frag (Varyings input) : SV_Target
            {
                float gridLines = computeGridLinesWithAA(input.modelPos);
                float borders = computeBordersWithAA(input.objPos);
                float alpha = max(gridLines, borders);
                
                return half4(_GridColor.rgb, _GridColor.a * alpha);
            }
            ENDHLSL
        }
    }
}
