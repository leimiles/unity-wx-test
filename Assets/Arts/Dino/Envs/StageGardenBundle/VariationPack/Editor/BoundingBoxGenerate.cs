#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class UniformUVBoundingBoxGenerator : EditorWindow
{
    private GameObject selectedModel;
    public string savePath = "Assets/WTest"; 

    [MenuItem("Tools/Generate Bounding Box")]
    public static void ShowWindow()
    {
        GetWindow<UniformUVBoundingBoxGenerator>("BoundingBox Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Uniform UV Bounding Box Generator", EditorStyles.boldLabel);

        selectedModel = (GameObject)EditorGUILayout.ObjectField("Target Model", selectedModel, typeof(GameObject), true);
        
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Bounding Box Mesh"))
        {
            if (selectedModel == null)
            {
                EditorUtility.DisplayDialog("Error", "请先选择模型!", "OK");
                return;
            }

            if (!Directory.Exists(savePath))
            {
                EditorUtility.DisplayDialog("Error", $"路径不存在: {savePath}", "OK");
                return;
            }

            GenerateUniformUVBoundingBox();
        }
    }

    private void GenerateUniformUVBoundingBox()
    {
        try
        {
            Bounds bounds = CalculateModelBounds(selectedModel);

            Mesh boundingBoxMesh = CreateUniformUVCubeMesh(bounds);

            string meshName = $"{selectedModel.name}_BoundingBox.asset";
            string meshPath = Path.Combine(savePath, meshName);
            meshPath = AssetDatabase.GenerateUniqueAssetPath(meshPath);

            AssetDatabase.CreateAsset(boundingBoxMesh, meshPath);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Success", $"包围盒Mesh已保存到:\n{meshPath}", "OK");

            Selection.activeObject = boundingBoxMesh;
            EditorGUIUtility.PingObject(boundingBoxMesh);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"生成包围盒时出错:\n{e.Message}", "OK");
        }
    }

    private Bounds CalculateModelBounds(GameObject model)
    {
        List<Vector3> allVertices = new List<Vector3>();

        foreach (MeshFilter mf in model.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh != null)
            {
                allVertices.AddRange(mf.sharedMesh.vertices);
            }
        }

        foreach (SkinnedMeshRenderer smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (smr.sharedMesh != null)
            {
                allVertices.AddRange(smr.sharedMesh.vertices);
            }
        }

        if (allVertices.Count == 0)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        Vector3 min = allVertices[0];
        Vector3 max = allVertices[0];
        
        foreach (Vector3 vertex in allVertices)
        {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
        }

        Vector3 size = max - min;
        Vector3 center = new Vector3(0, size.y / 2, 0); 

        return new Bounds(center, size);
    }

    private Mesh CreateUniformUVCubeMesh(Bounds bounds)
    {
        Mesh mesh = new Mesh();
        mesh.name = "UniformUVBoundingBox";

        Vector3 size = bounds.size;
        Vector3 halfSize = size * 0.5f;

        Vector3[] vertices = new Vector3[24];
        Vector2[] uv = new Vector2[24];
        
        vertices[0] = new Vector3(-halfSize.x, 0, -halfSize.z);
        vertices[1] = new Vector3( halfSize.x, 0, -halfSize.z);
        vertices[2] = new Vector3( halfSize.x, 0,  halfSize.z);
        vertices[3] = new Vector3(-halfSize.x, 0,  halfSize.z);
        for (int i = 0; i < 4; i++) uv[i] = new Vector2(vertices[i].x + halfSize.x, vertices[i].z + halfSize.z) / size.x;

        vertices[4] = new Vector3(-halfSize.x, size.y, -halfSize.z);
        vertices[5] = new Vector3( halfSize.x, size.y, -halfSize.z);
        vertices[6] = new Vector3( halfSize.x, size.y,  halfSize.z);
        vertices[7] = new Vector3(-halfSize.x, size.y,  halfSize.z);
        for (int i = 4; i < 8; i++) uv[i] = new Vector2(vertices[i].x + halfSize.x, vertices[i].z + halfSize.z) / size.x;

        vertices[8] = new Vector3(-halfSize.x, 0, halfSize.z);
        vertices[9] = new Vector3( halfSize.x, 0, halfSize.z);
        vertices[10] = new Vector3( halfSize.x, size.y, halfSize.z);
        vertices[11] = new Vector3(-halfSize.x, size.y, halfSize.z);
        for (int i = 8; i < 12; i++) uv[i] = new Vector2(vertices[i].x + halfSize.x, vertices[i].y) / size.x;

        vertices[12] = new Vector3( halfSize.x, 0, -halfSize.z);
        vertices[13] = new Vector3(-halfSize.x, 0, -halfSize.z);
        vertices[14] = new Vector3(-halfSize.x, size.y, -halfSize.z);
        vertices[15] = new Vector3( halfSize.x, size.y, -halfSize.z);
        for (int i = 12; i < 16; i++) uv[i] = new Vector2(vertices[i].x + halfSize.x, vertices[i].y) / size.x;

        vertices[16] = new Vector3(-halfSize.x, 0, -halfSize.z);
        vertices[17] = new Vector3(-halfSize.x, 0, halfSize.z);
        vertices[18] = new Vector3(-halfSize.x, size.y, halfSize.z);
        vertices[19] = new Vector3(-halfSize.x, size.y, -halfSize.z);
        for (int i = 16; i < 20; i++) uv[i] = new Vector2(vertices[i].z + halfSize.z, vertices[i].y) / size.x;

        vertices[20] = new Vector3(halfSize.x, 0, halfSize.z);
        vertices[21] = new Vector3(halfSize.x, 0, -halfSize.z);
        vertices[22] = new Vector3(halfSize.x, size.y, -halfSize.z);
        vertices[23] = new Vector3(halfSize.x, size.y, halfSize.z);
        for (int i = 20; i < 24; i++) uv[i] = new Vector2(vertices[i].z + halfSize.z, vertices[i].y) / size.x;

        int[] triangles = new int[36]
        {
            // 底面
            0, 2, 1,
            0, 3, 2,
            
            // 顶面
            4, 5, 6,
            4, 6, 7,
            
            // 前面
            8, 9, 10,
            8, 10, 11,
            
            // 后面
            12, 13, 14,
            12, 14, 15,
            
            // 左面
            16, 17, 18,
            16, 18, 19,
            
            // 右面
            20, 21, 22,
            20, 22, 23
        };

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        return mesh;
    }
}
#endif
