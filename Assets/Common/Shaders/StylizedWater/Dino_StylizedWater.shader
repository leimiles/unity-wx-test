Shader "Dino/Water/StylizedWater"
{
    Properties
    {
        [MaterialEnum(Off, 0, Front, 1, Back, 2)]_Cull("Cull Face", Float) = 1
        //Rendering
        [Header(General)]
        [Space(5)]
        [MaterialEnum(Mesh UV,0,World XZ projected ,1)]_WorldSpaceUV("UV Coordinates", Float) = 1
        _Speed("Animation Speed", Float) = 1
        _Direction("Animation direction", Vector) = (0,-1,0,0)

        [Header(Lighting)]
        [Space(5)]
        [ToggleOff(_UNLIT)] _LightingOn("Enable lighting", Float) = 1
        [ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadows("Recieve Shadows", Float) = 1
        _ShadowStrength("Shadow Strength", Range(0 , 1)) = 1
        _SparkleIntensity("Sparkle Intensity", Range(0 , 10)) = 00
		_SparkleSize("Sparkle Size", Range( 0 , 1)) = 0.280
        
        //Light Reflections
        [Header(Light Reflections)]
        [Space(5)]
        [ToggleOff(_SPECULARHIGHLIGHTS_OFF)] _SpecularReflectionsOn("Enable Specular", Float) = 1
        _SunReflectionStrength("Sun Strength", Float) = 10
        [PowerSlider(0.1)] _SunReflectionSize("Sun Size", Range(0 , 1)) = 0.5
        _SunReflectionDistortion("Sun Distortion", Range(0 ,2)) = 0.49

        //Color + Transparency
        [Header(WaterColor)]
        [Space(5)]
        [HDR]_BaseColor("Deep", Color) = (0, 0.44, 0.62, 1)
        [HDR]_ShallowColor("Shallow", Color) = (0.1, 0.9, 0.89, 0.02)

        _DepthVertical("View Depth", Range(0.01 , 16)) = 4
        _DepthHorizontal("Vertical Height Depth", Range(0.01 , 8)) = 1
        [Toggle] _DepthExp("Exponential Blend", Float) = 1

        [PowerSlider(3)] _ColorAbsorption("Color Absorption", Range(0 , 1)) = 0
        [Toggle] _VertexColorDepth("Vertex color (G) depth", Float) = 0
        _EdgeFade("Edge Fade", Float) = 0.1

        [HDR]_HorizonColor("Horizon", Color) = (0.84, 1, 1, 0.15)
        _HorizonDistance("Horizon Distance", Range(0.01 , 32)) = 8
        _WaveTint("Wave tint", Range( -0.1 , 0.1)) = 0

        //Normals
        [Header(WaterNormal)]
        [Space(5)]
        [Toggle(_NORMALMAP)] _NormalMapOn("Enable Normal maps", Float) = 1
        _NormalStrength("Strength", Range(0 , 1)) = 0.135
        [NoScaleOffset][Normal][SingleLineTexture]_BumpMap("Normals", 2D) = "bump" {}
        _NormalTiling("Tiling", Float) = 1
        _NormalSubTiling("Tiling (sub-layer)", Float) = 0.5
        _NormalSpeed("Speed multiplier", Float) = 0.2
        _NormalSubSpeed("Speed multiplier (sub-layer)", Float) = -0.5

        //Underwater
        [Header(Underwater)]
        [Space(5)]
        [Toggle(_CAUSTICS)] _CausticsOn("Enable Caustics", Float) = 1
        [NoScaleOffset][SingleLineTexture]_CausticsTex("Caustics RGB", 2D) = "black" {}
        _CausticsBrightness("Brightness", Float) = 2
        _CausticsDistortion("Distortion", Range(0, 1)) = 0.15
        _CausticsTiling("Tiling", Float) = 0.5
        _CausticsSpeed("Speed multiplier", Float) = 0.1

        //Intersection Foam
        [Header(Intersection Foam)]
        [Space(5)]
        [Toggle(_INTERSECTION_ON)] _IntersectionOn("Enable Intersection", Float) = 0
        [MaterialEnum(Depth Texture,0,Vertex Color (R),1,Depth Texture and Vertex Color,2)] _IntersectionSource("Intersection source", Float) = 0

        [NoScaleOffset][SingleLineTexture]_IntersectionNoise("Intersection noise", 2D) = "white" {}
        [hdr]_IntersectionColor("Color", Color) = (1,1,1,1)
        _IntersectionLength("Distance", Range(0.01 , 5)) = 2
        _IntersectionFalloff("Falloff", Range(0.01 , 1)) = 0.5
        _IntersectionTiling("Noise Tiling", float) = 0.2
        _IntersectionSpeed("Speed multiplier", float) = 0.1
        _IntersectionClipping("Cutoff", Range(0.01, 1)) = 0.5
        _IntersectionRippleDist("Ripple distance", float) = 32
        _IntersectionRippleStrength("Ripple Strength", Range(0 , 1)) = 0.5

        //Waves
        [Header(Waves)]
        [Space(5)]
        [Toggle(_WAVES)] _WavesOn("Enable Waves", Float) = 0
        _WaveSpeed("Speed", Float) = 2
        [Toggle] _VertexColorWaveFlattening("Vertex color (B) wave flattening", Float) = 0
        _WaveHeight("Height", Range(0 , 10)) = 0.25
        _WaveCount("Count", Range(1 , 5)) = 1
        _WaveDirection("Direction", vector) = (1,1,1,1)
        _WaveDistance("Distance", Range(0 , 1)) = 0.8
        _WaveSteepness("Steepness", Range(0 , 5)) = 0.1
        _WaveNormalStr("Normal Strength", Range(0 , 32)) = 0.5
        _WaveFadeDistance("Wave fade distance (Start/End)", Vector) = (150, 300, 0, 0)

        //Keyword states
        [Header(Rendering)]
        [Space(5)]
        [Toggle(_DISABLE_DEPTH_TEX)] _DisableDepthTexture("Disable depth texture", Float) = 0
        [MainTexture] [HideInInspector] _BaseMap("Albedo", 2D) = "white" {}
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
        [HideInInspector]_VertexColorFoam("Enable Waves", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "Queue" = "Transparent-99"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull[_Cull]
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma multi_compile_instancing

            //Unity defined keywords
            // #pragma multi_compile_fog
            // #pragma multi_compile _ LIGHTMAP_ON
            // #pragma multi_compile _ DYNAMICLIGHTMAP_ON

            // Defines
            #define _SURFACE_TYPE_TRANSPARENT 1
            #define _SMOOTH_INTERSECTION 1
            // #define UnityFog 1
            // #define _ADVANCED_SHADING 1

            // Material Keywords
            // #define _NORMALMAP 1
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _ _WAVES
            #pragma shader_feature_local _ _INTERSECTION_ON
            // #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _DISABLE_DEPTH_TEX
            #pragma shader_feature_local_fragment _UNLIT
            #pragma shader_feature_local_fragment _CAUSTICS
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            // #pragma shader_feature_local_fragment _ADVANCED_SHADING
            // #pragma shader_feature_local_fragment _ _SHARP_INERSECTION _SMOOTH_INTERSECTION

            #include "./Lib/URP.hlsl"
            #include "./Lib/Input.hlsl"
            #include "./Lib/Common.hlsl"
            #include "./Lib/Waves.hlsl"
            #include "./Lib/Lighting.hlsl"
            #include "./Lib/Features.hlsl"
            #include "./Lib/Caustics.hlsl"
            #include "./Lib/Vertex.hlsl"

            #pragma vertex LitPassVertex
            #pragma fragment ForwardPassFragment
            #include "./Lib/ForwardPass.hlsl"
            ENDHLSL
        }
    }
}