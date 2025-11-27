using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class ShaderVariantCollector : EditorWindow
{
    public ShaderVariantCollection variantCollection;
    public GameObject[] characterPrefabs;

    [MenuItem("Tools/Collect Character Shader Variants")]
    static void Open()
    {
        GetWindow<ShaderVariantCollector>("Shader Variant Collector");
    }

    void OnGUI()
    {
        variantCollection = (ShaderVariantCollection)EditorGUILayout.ObjectField(
            "Variant Collection",
            variantCollection,
            typeof(ShaderVariantCollection),
            false
        );

        SerializedObject so = new SerializedObject(this);
        SerializedProperty arrayProp = so.FindProperty("characterPrefabs");
        EditorGUILayout.PropertyField(arrayProp, true);
        so.ApplyModifiedProperties();

        if (GUILayout.Button("Collect Variants"))
        {
            Collect();
        }
    }

    void Collect()
    {
        if (variantCollection == null)
        {
            Debug.LogError("Please assign a ShaderVariantCollection asset.");
            return;
        }

        variantCollection.Clear();

        var renderers = new List<Renderer>();

        foreach (var prefab in characterPrefabs)
        {
            if (prefab == null) continue;
            var rs = prefab.GetComponentsInChildren<Renderer>(true);
            renderers.AddRange(rs);
        }

        foreach (var r in renderers)
        {
            foreach (var mat in r.sharedMaterials)
            {
                if (mat == null || mat.shader == null) continue;

                // 最简单：只收集一个“默认” Pass 的一个变体
                var variant = new ShaderVariantCollection.ShaderVariant(
                    mat.shader,
                    PassType.ForwardBase
                );

                variantCollection.Add(variant);
            }
        }

        EditorUtility.SetDirty(variantCollection);
        AssetDatabase.SaveAssets();
        Debug.Log("Shader variants collected.");
    }
}
