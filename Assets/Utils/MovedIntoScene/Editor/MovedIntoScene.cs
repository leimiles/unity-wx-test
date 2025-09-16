using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
///  这是一个工具，用于将工程中选中的 prefab 放置（instantiate）到当前场景中
///  自动避免多对象放置时的重叠，所有对象会根据预设的行距和列距离进行排列
///  使用时，选中工程中面板中需要放置的 prefab，然后右键菜单，选择 "Utils /  Moved Into Scene"
/// </summary>
public class MovedIntoScene : Editor
{
    static GameObject spawnRoot;   // 所有实例化对象的父物体
    static Vector3 centerPos = Vector3.zero; // 起始位置，第一个对象出现的位置
    static float rowDistance = 10f; // 行距，x 轴向的间隔值，y 轴不变，z 轴不变
    static float colDistance = 10f; // 列距，z 轴向的间隔值，x 轴不变，y 轴不变

    // 在工程面板的右键菜单中添加一个按钮，用户可以通过右键菜单，选择 Utils/ Moved Into Scene 来执行该功能
    [MenuItem("Assets/Utils/Moved Into Scene", false, 100)]
    static void InstantiateSelectedPrefabs()
    {
        // 得到当前所选的所有对象
        Object[] selections = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
        // 如果没有选中任何对象，则提示用户
        if (selections.Length == 0)
        {
            Debug.LogWarning("No prefab selected. Please select one or more prefabs in the Project window.");
            return;
        }

        // 创建一个空物体作为所有实例化对象的父物体，不用管是否重复，用户如果发现重复，可以自己删除
        spawnRoot = new GameObject("SpawnedRoot");

        // 通过对象总数，计算需要多少行和列，整体排列成一个接近正方形的矩形
        int totalCount = selections.Length;
        int cols = Mathf.CeilToInt(Mathf.Sqrt(totalCount)); // 列数，向上取整
        int rows = Mathf.CeilToInt((float)totalCount / cols); // 行数，向上取整


        for (int i = 0; i < totalCount; i++)
        {
            centerPos.x = (i % cols) * rowDistance; // 计算 x 位置
            centerPos.z = (i / cols) * colDistance; // 计算 z 位置
            centerPos.y = 0; // y 位置保持不变

            // 实例化选中的 prefab
            GameObject prefab = selections[i] as GameObject;
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance != null)
            {
                // 设置实例化对象的位置和父物体
                instance.transform.position = centerPos;
                instance.transform.SetParent(spawnRoot.transform);
                // 选中该实例化对象
                Selection.activeGameObject = instance;
            }
            else
            {
                Debug.LogError($"Failed to instantiate prefab: {prefab.name}");
            }
        }
    }
}
