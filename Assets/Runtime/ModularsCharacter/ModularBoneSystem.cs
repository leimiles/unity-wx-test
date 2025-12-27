using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 模块化角色骨骼管理系统
/// 职责：管理骨骼映射、验证、重置、重绑定等骨骼相关逻辑
/// </summary>
public class ModularBoneSystem
{
    // 性能优化：缓存最后一次查询的根骨骼和对应的 Transform 数组
    // 注意：这个缓存在更换角色骨骼根时会自动失效
    private Transform _lastBonesRoot;
    private Transform[] _boneTransformCache;
    /// <summary>
    /// 验证并初始化骨骼映射
    /// </summary>
    /// <param name="modularChar">模块化角色数据</param>
    /// <returns>是否成功验证</returns>
    public bool VerifyBoneMap(IModularChar modularChar)
    {
        if (modularChar == null)
        {
            Debug.LogWarning("[ModularBoneSystem] ModularChar is null");
            return false;
        }

        if (modularChar.BaseBonesRoot == null)
        {
            Debug.LogWarning("[ModularBoneSystem] BaseBonesRoot is null");
            modularChar.SetBoneMapVerified(false);
            return false;
        }

        // 清空现有映射
        modularChar.InternalBonesMap.Clear();
        modularChar.InternalOriginalBonesMap.Clear();

        // 性能优化：只有在根骨骼改变时才重新获取 Transform 数组
        if (_lastBonesRoot != modularChar.BaseBonesRoot)
        {
            _boneTransformCache = modularChar.BaseBonesRoot.GetComponentsInChildren<Transform>();
            _lastBonesRoot = modularChar.BaseBonesRoot;
        }

        // 遍历所有子骨骼
        foreach (var bone in _boneTransformCache)
        {
            modularChar.InternalBonesMap[bone.name] = bone;
            modularChar.InternalOriginalBonesMap[bone.name] = bone; // 保存原始引用
        }

        modularChar.SetBoneMapVerified(true);
        return true;
    }

    /// <summary>
    /// 重置骨骼映射，移除之前添加的额外骨骼（保留原始基准骨骼）
    /// </summary>
    /// <param name="modularChar">模块化角色数据</param>
    public void ResetBoneMap(IModularChar modularChar)
    {
        if (modularChar == null || !modularChar.IsBoneMapVerified)
        {
            Debug.LogWarning("[ModularBoneSystem] Cannot reset bone map: not verified");
            return;
        }

        var bonesMap = modularChar.InternalBonesMap;
        var originalBonesMap = modularChar.InternalOriginalBonesMap;

        // 找出所有额外骨骼（不在原始映射中的）
        List<string> extraBones = new List<string>();
        foreach (var kvp in bonesMap)
        {
            if (!originalBonesMap.ContainsKey(kvp.Key))
            {
                extraBones.Add(kvp.Key);
            }
        }

        // 移除额外骨骼
        foreach (var boneName in extraBones)
        {
            bonesMap.Remove(boneName);
        }

        // 确保所有原始骨骼都在映射中
        foreach (var kvp in originalBonesMap)
        {
            bonesMap[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// 重新绑定 SkinnedMeshRenderer 的骨骼，兼容额外骨骼（如尾巴等）
    /// </summary>
    /// <param name="renderer">SkinnedMeshRenderer 组件</param>
    /// <param name="modularChar">模块化角色数据</param>
    /// <returns>是否成功重绑定</returns>
    public bool RebindBones(SkinnedMeshRenderer renderer, IModularChar modularChar)
    {
        if (renderer == null)
        {
            Debug.LogWarning("[ModularBoneSystem] SkinnedMeshRenderer is null");
            return false;
        }

        if (modularChar == null || !modularChar.IsBoneMapVerified)
        {
            Debug.LogWarning("[ModularBoneSystem] Cannot rebind bones: bone map not verified");
            return false;
        }

        if (modularChar.BaseBonesRoot == null)
        {
            Debug.LogWarning("[ModularBoneSystem] BaseBonesRoot is null");
            return false;
        }

        var bonesMap = modularChar.InternalBonesMap;
        Transform[] sourceBones = renderer.bones;
        Transform[] newBones = new Transform[sourceBones.Length];
        Transform rootBone = renderer.rootBone;

        // 处理每个骨骼
        for (int i = 0; i < sourceBones.Length; i++)
        {
            Transform sourceBone = sourceBones[i];
            if (sourceBone == null)
            {
                newBones[i] = null;
                continue;
            }

            // 检查是否是标准骨骼
            if (bonesMap.TryGetValue(sourceBone.name, out Transform baseBone))
            {
                // 标准骨骼：使用基准骨骼
                newBones[i] = baseBone;
            }
            else
            {
                // 额外骨骼（如尾巴）：保留原骨骼，添加到映射中
                bonesMap[sourceBone.name] = sourceBone;

                // 处理父节点关系
                if (sourceBone.parent != null)
                {
                    if (bonesMap.TryGetValue(sourceBone.parent.name, out Transform parentBone))
                    {
                        // 父节点已在映射中，直接设置
                        sourceBone.SetParent(parentBone, false);
                    }
                    else
                    {
                        // 父节点不在映射中，递归处理
                        FindAndSetParentInBoneMap(sourceBone, bonesMap);
                    }
                }

                newBones[i] = sourceBone;
            }
        }

        // 应用新骨骼数组
        renderer.bones = newBones;

        // 设置根骨骼
        if (rootBone != null && bonesMap.TryGetValue(rootBone.name, out Transform newRootBone))
        {
            renderer.rootBone = newRootBone;
        }
        else
        {
            renderer.rootBone = modularChar.BaseBonesRoot;
        }

        return true;
    }

    /// <summary>
    /// 在基准骨骼映射中，找到当前骨骼的父节点，并设置当前骨骼的父节点
    /// </summary>
    private void FindAndSetParentInBoneMap(Transform bone, Dictionary<string, Transform> bonesMap)
    {
        Transform parent = bone.parent;
        if (parent == null) return;

        // 如果父节点不在映射中，递归处理父节点
        if (!bonesMap.ContainsKey(parent.name))
        {
            FindAndSetParentInBoneMap(parent, bonesMap);
            // 递归返回后，将父节点添加到映射
            bonesMap[parent.name] = parent;
        }

        // 设置当前骨骼的父节点
        if (bonesMap.TryGetValue(parent.name, out Transform parentBone))
        {
            bone.SetParent(parentBone, false);
        }
    }

    /// <summary>
    /// 移除旧骨骼（从装备部位中移除与基准骨骼根节点同名的 Transform）
    /// </summary>
    /// <param name="targetTransform">目标 Transform（通常是装备部位的根节点）</param>
    /// <param name="modularChar">模块化角色数据</param>
    public void RemoveOldBones(Transform targetTransform, IModularChar modularChar)
    {
        if (targetTransform == null || modularChar?.BaseBonesRoot == null)
        {
            return;
        }

        // 限制最大深度为 3 层，避免全局递归查找
        string targetName = modularChar.BaseBonesRoot.name;
        Transform oldRootBone = FindChildByNameWithMaxDepth(targetTransform, targetName, 3);

        if (oldRootBone != null)
        {
            Object.Destroy(oldRootBone.gameObject);
        }
    }

    /// <summary>
    /// 在指定深度内查找子节点
    /// </summary>
    /// <param name="parent">父节点</param>
    /// <param name="targetName">目标名称</param>
    /// <param name="maxDepth">最大深度（1 表示只查找直接子节点）</param>
    /// <returns>找到的 Transform，未找到返回 null</returns>
    private Transform FindChildByNameWithMaxDepth(Transform parent, string targetName, int maxDepth)
    {
        if (parent == null || maxDepth <= 0)
        {
            return null;
        }

        // 查找当前层级
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == targetName)
            {
                return child;
            }
        }

        // 递归查找下一层级
        if (maxDepth > 1)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Transform result = FindChildByNameWithMaxDepth(child, targetName, maxDepth - 1);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }
}

