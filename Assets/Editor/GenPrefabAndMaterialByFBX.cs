using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 在工程中选中一个 FBX 文件
/// 右键 m_utils 菜单，选择 GenPrefabAndMaterialByFBX
/// 可以生成一个 prefab 和对应的 material
/// 生成位置位于相同目录
/// 命名规则为：{FBX文件所在目录的上级目录文件名}@{FBX文件名}.prefab 和 {FBX文件所在目录的上级目录文件名}@{FBX文件名}.mat
/// 注意，.fbx 文件应该位于 /FBX 文件夹下，/FBX 文件夹应位于独立内容的文件夹下，例如 JiJiGuoWang/FBX
/// 因此，如果有 JiJiGuoWang/FBX/Suit216.fbx 文件
/// JiJiGuoWang/FBX/JiJiGuoWang@Suit216.prefab
/// JiJiGuoWang/FBX/JiJiGuoWang@Suit216.mat
/// </summary>
public class GenPrefabAndMaterialByFBX : Editor
{

    static string targetPath = "";
    static string ContentFolderName = "";

    static Object selectedFBXObject = null;
    static string generatedPrefabPath = "";

    [MenuItem("Assets/M_Utils/GenPrefabAndMaterialByFBX", false, 101)]
    static void Gen()
    {
        // 检查当前选择
        if (ValidateSelection())
        {
            // 生成指定名称的 prefab
            GeneratePrefab();

            // 生成指定名称的 material
            GenerateMaterial();
        }

    }

    /// <summary>
    /// 检查当前所选资源是否为 .fbx 文件
    /// 为 targetPath 赋值，即当前 .fbx 文件所在的目录
    /// 获取 ContentFolderName 为 .fbx 文件所在目录的上级目录文件名
    /// 例如，如果选中的是 JiJiGuoWang/FBX/Suit216.fbx，则 ContentFolderName 为 JiJiGuoWang
    /// 先执行到这一步输出 debug 我看看结果
    /// </summary>
    static bool ValidateSelection()
    {
        // 获取当前选中的对象
        selectedFBXObject = Selection.activeObject;

        if (selectedFBXObject == null)
        {
            Debug.LogError("未选中任何资源");
            return false;
        }

        // 获取选中资源的路径
        string assetPath = AssetDatabase.GetAssetPath(selectedFBXObject);

        // 检查是否为 .fbx 文件
        if (!assetPath.ToLower().EndsWith(".fbx"))
        {
            Debug.LogError($"选中的资源不是 .fbx 文件: {assetPath}");
            return false;
        }

        // 获取文件所在目录（targetPath）
        targetPath = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');

        // 获取目录的上级目录文件名（ContentFolderName）
        // 例如：Assets/Arts/Sausages/Characters/JiJiGuoWang/FBX -> JiJiGuoWang
        string[] pathParts = targetPath.Split('/');
        if (pathParts.Length >= 2)
        {
            // 获取倒数第二个目录名（FBX 的上级目录）
            ContentFolderName = pathParts[pathParts.Length - 2];
        }
        else
        {
            Debug.LogError($"路径格式不正确，无法获取 ContentFolderName: {targetPath}");
            return false;
        }

        // 输出 debug 信息
        Debug.Log($"选中 FBX 文件: {assetPath}");
        Debug.Log($"目标路径 (targetPath): {targetPath}");
        Debug.Log($"内容文件夹名 (ContentFolderName): {ContentFolderName}");
        return true;
    }

    /// <summary>
    /// 基于新的名称，将 selectedFBXObject 生成到场景中
    /// 并在目标路径下生成 prefab 文件
    /// </summary>
    static void GeneratePrefab()
    {
        if (selectedFBXObject == null)
        {
            Debug.LogError("selectedFBXObject 为空，无法生成 Prefab");
            return;
        }

        // 获取 FBX 文件名（不含扩展名）
        string fbxAssetPath = AssetDatabase.GetAssetPath(selectedFBXObject);
        string fbxFileName = System.IO.Path.GetFileNameWithoutExtension(fbxAssetPath);

        // 构建新的 prefab 名称：ContentFolderName@{FBX文件名}
        string prefabName = $"{ContentFolderName}@{fbxFileName}";
        string prefabPath = $"{targetPath}/{prefabName}.prefab";

        // 实例化 FBX 到场景中
        GameObject instance = null;
        if (selectedFBXObject is GameObject)
        {
            // 如果是 GameObject，直接实例化
            instance = (GameObject)PrefabUtility.InstantiatePrefab(selectedFBXObject);
        }
        else
        {
            // 如果是模型资源，需要先加载为 GameObject
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxAssetPath);
            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            }
            else
            {
                Debug.LogError($"无法从路径加载 GameObject: {fbxAssetPath}");
                return;
            }
        }

        if (instance == null)
        {
            Debug.LogError("实例化 FBX 对象失败");
            return;
        }

        // 设置实例名称
        instance.name = prefabName;

        // 将实例添加到当前场景
        UnityEngine.SceneManagement.Scene activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(instance, activeScene);

        // 保存为 prefab 文件
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);

        if (prefabAsset != null)
        {
            Debug.Log($"成功生成 Prefab: {prefabPath}");
            generatedPrefabPath = prefabPath;

            // 选中生成的 prefab
            Selection.activeObject = prefabAsset;
            EditorGUIUtility.PingObject(prefabAsset);
        }
        else
        {
            Debug.LogError($"保存 Prefab 失败: {prefabPath}");
        }

        // 清理场景中的临时实例（可选，如果需要保留在场景中则注释掉）
        Object.DestroyImmediate(instance);
    }


    /// <summary>
    /// 要基于这个生成在场景中的 prefab，来判断需要生成多少个材质
    /// 首先要判断有多少个 renderer 组件，每个 renderer 至少生成一个材质
    /// 如果 renderer 的 sharedMaterials 长度大于 1，则需要生成多个材质，且名称带有后缀 _1, _2, _3, ...
    /// 举例
    /// 如果 prefab 中包含 2 个 renderer 组件，且 renderer 1 的 sharedMaterials 长度为 2，renderer 2 的 sharedMaterials 长度为 1
    /// 则需要生成 3 个材质，名称分别为 {ContentFolderName}@{FBX文件名}_1, {ContentFolderName}@{FBX文件名}_2, {ContentFolderName}@{FBX文件名}_3
    /// 其中，{ContentFolderName}@{FBX文件名}_1 为 renderer 1 的 sharedMaterials[0]
    /// {ContentFolderName}@{FBX文件名}_2 为 renderer 1 的 sharedMaterials[1]
    /// {ContentFolderName}@{FBX文件名}_3 为 renderer 2 的 sharedMaterials[0]
    /// 注意，如果 renderer 的 sharedMaterials 长度为 0，则不生成材质
    /// 注意，如果 renderer 的 sharedMaterials 长度为 1，则直接使用 sharedMaterials[0] 的名称
    /// 注意，如果 renderer 的 sharedMaterials 长度大于 1，则需要生成多个材质，且名称带有后缀 _1, _2, _3, ...
    /// 注意，如果 renderer 的 sharedMaterials 长度大于 1，则需要生成多个材质，且名称带有后缀 _1, _2, _3, ...
    /// </summary>
    static void GenerateMaterial()
    {
        if (string.IsNullOrEmpty(generatedPrefabPath))
        {
            Debug.LogError("生成的 Prefab 路径为空，无法生成 Material");
            return;
        }

        // 加载生成的 prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(generatedPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"无法加载 Prefab: {generatedPrefabPath}");
            return;
        }

        // 获取 FBX 文件名（不含扩展名）
        string fbxAssetPath = AssetDatabase.GetAssetPath(selectedFBXObject);
        string fbxFileName = System.IO.Path.GetFileNameWithoutExtension(fbxAssetPath);

        // 获取所有 Renderer 组件（包括子对象）
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning($"Prefab 中没有找到 Renderer 组件: {generatedPrefabPath}");
            return;
        }

        // 收集所有需要复制的材质
        List<Material> materialsToCopy = new List<Material>();
        List<string> materialNames = new List<string>();
        int materialIndex = 1;

        foreach (Renderer renderer in renderers)
        {
            if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
            {
                // 如果 renderer 的 sharedMaterials 长度为 0，则不生成材质
                continue;
            }

            // 遍历该 renderer 的所有材质
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                Material originalMaterial = renderer.sharedMaterials[i];
                if (originalMaterial != null)
                {
                    materialsToCopy.Add(originalMaterial);
                    // 生成带后缀的名称：{ContentFolderName}@{FBX文件名}_1, _2, _3, ...
                    // 即使长度为 1，也使用带后缀的名称（根据示例，renderer 2 的材质命名为 _3）
                    string materialName = $"{ContentFolderName}@{fbxFileName}_{materialIndex}";
                    materialNames.Add(materialName);
                    materialIndex++;
                }
            }
        }

        if (materialsToCopy.Count == 0)
        {
            Debug.LogWarning("没有找到需要复制的材质");
            return;
        }

        // 复制并保存材质
        List<Material> generatedMaterials = new List<Material>();
        for (int i = 0; i < materialsToCopy.Count; i++)
        {
            Material originalMaterial = materialsToCopy[i];
            string materialName = materialNames[i];
            string materialPath = $"{targetPath}/{materialName}.mat";

            // 复制材质
            Material newMaterial = new Material(originalMaterial);
            newMaterial.name = materialName;

            // 保存材质
            AssetDatabase.CreateAsset(newMaterial, materialPath);
            generatedMaterials.Add(newMaterial);

            Debug.Log($"成功生成 Material: {materialPath} (基于: {originalMaterial.name})");
        }

        // 刷新资源数据库
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 选中最后一个生成的材质
        if (generatedMaterials.Count > 0)
        {
            Selection.activeObject = generatedMaterials[generatedMaterials.Count - 1];
            EditorGUIUtility.PingObject(generatedMaterials[generatedMaterials.Count - 1]);
        }

        Debug.Log($"总共生成了 {generatedMaterials.Count} 个材质");
    }
}
