using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

internal class MiniLitShaderGUI : ShaderGUI
{
    private string[] renderingModes = new string[] {
        "Normal",
        "Debug_Albedo",
        "Debug_Normal",
        "Debug_Metallic",
        "Debug_AO",
        "Debug_Roughness",
        "Debug_Emission",
        "Debug_Light",
        "Debug_BakedGI"
        };
    private int selectedRenderingMode = 0;

    private MaterialProperty _StencilRef;
    private MaterialProperty _StencilComp;
    private MaterialProperty _StencilPass;
    private MaterialProperty _StencilFail;
    private MaterialProperty _StencilZFail;

    private MaterialProperty _ZTest;
    private MaterialProperty _ZWrite;
    private MaterialProperty _SrcBlend;
    private MaterialProperty _DstBlend;
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        FindStencilProperties(properties);
        FindDepthAndBlendProperties(properties);
        base.OnGUI(materialEditor, properties);
        materialEditor.EmissionEnabledProperty();
        DrawDebugArea(materialEditor);
        EditorGUILayout.LabelField("Stencil Settings (stencil breaks batch)", EditorStyles.boldLabel);
        DrawStencilGUI(materialEditor);
        EditorGUILayout.LabelField("Z Settings (z breaks batch)", EditorStyles.boldLabel);
        DrawZTest();
        DrawZWrite();
        //EditorGUILayout.LabelField("Blend Settings (blend breaks batch)", EditorStyles.boldLabel);
        //DrawBlend("Src Blend", _SrcBlend);
        //DrawBlend("Dst Blend", _DstBlend);
    }

    private void FindStencilProperties(MaterialProperty[] props)
    {
        _StencilRef = FindProperty("_StencilRef", props);
        _StencilComp = FindProperty("_StencilComp", props);
        _StencilPass = FindProperty("_StencilPass", props);
        _StencilFail = FindProperty("_StencilFail", props);
        _StencilZFail = FindProperty("_StencilZFail", props);
    }

    private void FindDepthAndBlendProperties(MaterialProperty[] props)
    {
        _ZTest = FindProperty("_ZTest", props);
        _ZWrite = FindProperty("_ZWrite", props);
        _SrcBlend = FindProperty("_SrcBlend", props);
        _DstBlend = FindProperty("_DstBlend", props);
    }

    private void DrawZTest()
    {
        // 把 float 转成 CompareFunction 枚举
        CompareFunction oldVal = (CompareFunction)(int)_ZTest.floatValue;
        CompareFunction newVal = (CompareFunction)EditorGUILayout.EnumPopup("ZTest", oldVal);
        if (newVal != oldVal)
        {
            _ZTest.floatValue = (float)newVal;
        }
    }

    private void DrawZWrite()
    {
        // 这里用 Toggle 来开关 0 or 1
        bool oldVal = (_ZWrite.floatValue > 0.5f);
        bool newVal = EditorGUILayout.Toggle("ZWrite", oldVal);
        if (newVal != oldVal)
        {
            _ZWrite.floatValue = newVal ? 1f : 0f;
        }
    }

    private void DrawBlend(string label, MaterialProperty prop)
    {
        // 把 float 转成 BlendMode 枚举
        BlendMode oldVal = (BlendMode)(int)prop.floatValue;
        BlendMode newVal = (BlendMode)EditorGUILayout.EnumPopup(label, oldVal);
        if (newVal != oldVal)
        {
            prop.floatValue = (float)newVal;
        }
    }

    private void DrawStencilGUI(MaterialEditor materialEditor)
    {
        // 1) Stencil Ref (int/float)
        materialEditor.ShaderProperty(_StencilRef, "Stencil Ref (0 - 255)");

        // 2) Stencil CompareFunction
        CompareFunction comp = (CompareFunction)(int)_StencilComp.floatValue;
        CompareFunction newComp = (CompareFunction)EditorGUILayout.EnumPopup("Stencil Comp", comp);
        if (newComp != comp)
        {
            _StencilComp.floatValue = (float)newComp;
        }

        // 3) Stencil Pass
        StencilOp passOp = (StencilOp)(int)_StencilPass.floatValue;
        StencilOp newPassOp = (StencilOp)EditorGUILayout.EnumPopup("Stencil Pass", passOp);
        if (newPassOp != passOp)
        {
            _StencilPass.floatValue = (float)newPassOp;
        }

        // 4) Stencil Fail
        StencilOp failOp = (StencilOp)(int)_StencilFail.floatValue;
        StencilOp newFailOp = (StencilOp)EditorGUILayout.EnumPopup("Stencil Fail", failOp);
        if (newFailOp != failOp)
        {
            _StencilFail.floatValue = (float)newFailOp;
        }

        // 5) Stencil ZFail
        StencilOp zFailOp = (StencilOp)(int)_StencilZFail.floatValue;
        StencilOp newZFailOp = (StencilOp)EditorGUILayout.EnumPopup("Stencil ZFail", zFailOp);
        if (newZFailOp != zFailOp)
        {
            _StencilZFail.floatValue = (float)newZFailOp;
        }
    }

    void DrawDebugArea(MaterialEditor materialEditor)
    {
        selectedRenderingMode = EditorGUILayout.Popup("Debug Option", selectedRenderingMode, renderingModes);
        Material targetMaterial = materialEditor.target as Material;
        switch (selectedRenderingMode)
        {
            case 0:
                // normal rendering
                targetMaterial.DisableKeyword("Debug_Albedo");
                targetMaterial.DisableKeyword("Debug_Normal");
                targetMaterial.DisableKeyword("Debug_Metallic");
                targetMaterial.DisableKeyword("Debug_AO");
                targetMaterial.DisableKeyword("Debug_Roughness");
                targetMaterial.DisableKeyword("Debug_Emission");
                targetMaterial.DisableKeyword("Debug_Light");
                targetMaterial.DisableKeyword("Debug_BakedGI");
                break;
            case 1:
                // debug albedo
                targetMaterial.EnableKeyword("Debug_Albedo");
                targetMaterial.DisableKeyword("Debug_Normal");
                targetMaterial.DisableKeyword("Debug_Metallic");
                targetMaterial.DisableKeyword("Debug_AO");
                targetMaterial.DisableKeyword("Debug_Roughness");
                targetMaterial.DisableKeyword("Debug_Emission");
                targetMaterial.DisableKeyword("Debug_Light");
                targetMaterial.DisableKeyword("Debug_BakedGI");
                break;
            case 2:
                // debug normal
                targetMaterial.DisableKeyword("Debug_Albedo");
                targetMaterial.EnableKeyword("Debug_Normal");
                targetMaterial.DisableKeyword("Debug_Metallic");
                targetMaterial.DisableKeyword("Debug_AO");
                targetMaterial.DisableKeyword("Debug_Roughness");
                targetMaterial.DisableKeyword("Debug_Emission");
                targetMaterial.DisableKeyword("Debug_Light");
                targetMaterial.DisableKeyword("Debug_BakedGI");
                break;
            case 3:
                // debug metallic
                targetMaterial.DisableKeyword("Debug_Albedo");
                targetMaterial.DisableKeyword("Debug_Normal");
                targetMaterial.EnableKeyword("Debug_Metallic");
                targetMaterial.DisableKeyword("Debug_AO");
                targetMaterial.DisableKeyword("Debug_Roughness");
                targetMaterial.DisableKeyword("Debug_Emission");
                targetMaterial.DisableKeyword("Debug_Light");
                targetMaterial.DisableKeyword("Debug_BakedGI");
                break;
            case 4:
                // debug ao
                targetMaterial.DisableKeyword("Debug_Albedo");
                targetMaterial.DisableKeyword("Debug_Normal");
                targetMaterial.DisableKeyword("Debug_Metallic");
                targetMaterial.EnableKeyword("Debug_AO");
                targetMaterial.DisableKeyword("Debug_Roughness");
                targetMaterial.DisableKeyword("Debug_Emission");
                targetMaterial.DisableKeyword("Debug_Light");
                targetMaterial.DisableKeyword("Debug_BakedGI");
                break;
            case 5:
                // debug roughness
                targetMaterial.DisableKeyword("Debug_Albedo");
                targetMaterial.DisableKeyword("Debug_Normal");
                targetMaterial.DisableKeyword("Debug_Metallic");
                targetMaterial.DisableKeyword("Debug_AO");
                targetMaterial.EnableKeyword("Debug_Roughness");
                targetMaterial.DisableKeyword("Debug_Emission");
                targetMaterial.DisableKeyword("Debug_Light");
                targetMaterial.DisableKeyword("Debug_BakedGI");
                break;
            case 6:
                // debug emission
                targetMaterial.DisableKeyword("Debug_Albedo");
                targetMaterial.DisableKeyword("Debug_Normal");
                targetMaterial.DisableKeyword("Debug_Metallic");
                targetMaterial.DisableKeyword("Debug_AO");
                targetMaterial.DisableKeyword("Debug_Roughness");
                targetMaterial.EnableKeyword("Debug_Emission");
                targetMaterial.DisableKeyword("Debug_Light");
                targetMaterial.DisableKeyword("Debug_BakedGI");
                break;
            case 7:
                // debug light color
                targetMaterial.DisableKeyword("Debug_Albedo");
                targetMaterial.DisableKeyword("Debug_Normal");
                targetMaterial.DisableKeyword("Debug_Metallic");
                targetMaterial.DisableKeyword("Debug_AO");
                targetMaterial.DisableKeyword("Debug_Roughness");
                targetMaterial.DisableKeyword("Debug_Emission");
                targetMaterial.EnableKeyword("Debug_Light");
                targetMaterial.DisableKeyword("Debug_BakedGI");
                break;
            case 8:
                // debug baked gi
                targetMaterial.DisableKeyword("Debug_Albedo");
                targetMaterial.DisableKeyword("Debug_Normal");
                targetMaterial.DisableKeyword("Debug_Metallic");
                targetMaterial.DisableKeyword("Debug_AO");
                targetMaterial.DisableKeyword("Debug_Roughness");
                targetMaterial.DisableKeyword("Debug_Emission");
                targetMaterial.DisableKeyword("Debug_Light");
                targetMaterial.EnableKeyword("Debug_BakedGI");
                break;
        }
    }


}

