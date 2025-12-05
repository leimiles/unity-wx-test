using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[DisallowMultipleComponent]
public class ModularChar : MonoBehaviour
{
    [SerializeField] Transform rigidAttachment;
    [SerializeField] GameObject rigidPrefabTest1;
    [SerializeField] GameObject rigidPrefabTest2;
    [SerializeField] Transform baseBonesRoot;
    [SerializeField] GameObject skinnedOutfitPrefabTest1;
    [SerializeField] GameObject skinnedOutfitPrefabTest2;
    Dictionary<string, Transform> baseBonesMap = new Dictionary<string, Transform>();
    Dictionary<string, Transform> baseBonesOriginal = new Dictionary<string, Transform>(); // 保存基准骨骼的原始引用（用于重置）

#if UNITY_EDITOR
    /// <summary>
    /// 在编辑器模式下，初始化时，更换服装
    /// </summary>
    void Start()
    {
        VerifyBoneMap();
        ChangeOutfit(skinnedOutfitPrefabTest1);
        ChangeOutfit(skinnedOutfitPrefabTest2);
    }
#endif


    void VerifyBoneMap()
    {
        if (baseBonesRoot != null)
        {
            baseBonesMap.Clear();
            baseBonesOriginal.Clear();

            foreach (var bone in baseBonesRoot.GetComponentsInChildren<Transform>())
            {
                baseBonesMap[bone.name] = bone;
                baseBonesOriginal[bone.name] = bone; // 保存原始基准骨骼引用
            }
        }
    }

    /// <summary>
    /// 重置骨骼映射，移除之前添加的额外骨骼
    /// </summary>
    void ResetBoneMap()
    {
        // 清理所有额外骨骼（不在原始基准骨骼中的）
        List<string> keysToRemove = new List<string>();
        foreach (var kvp in baseBonesMap)
        {
            if (!baseBonesOriginal.ContainsKey(kvp.Key))
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        // 移除额外骨骼
        foreach (var key in keysToRemove)
        {
            baseBonesMap.Remove(key);
        }

        // 确保所有原始基准骨骼都在映射中
        foreach (var kvp in baseBonesOriginal)
        {
            baseBonesMap[kvp.Key] = kvp.Value;
        }
    }


    /// <summary>
    /// 添加刚体部位，会删除旧的刚体部位，并实例化新的刚体部位
    /// </summary>
    /// <param name="rigidPartTransform"></param>
    /// <param name="rigidPrefab"></param>
    public void ChangeRigidPart(Transform rigidPartAttachment, GameObject rigidPrefab)
    {
        if (rigidPartAttachment == null || rigidPrefab == null) return;

    }


    GameObject currentHair; // 添加字段存储当前实例
    public void ChangeHair(GameObject rigidPrefab)
    {
        if (rigidPrefab == null)
        {
            Debug.LogWarning("Hair prefab is not assigned");
            return;
        }

        if (rigidAttachment == null)
        {
            Debug.LogWarning("Head attachment point is not assigned");
            return;
        }

        // 清理旧头发
        if (currentHair != null)
        {
            Destroy(currentHair);
        }

        // 实例化新头发
        currentHair = Instantiate(rigidPrefab, rigidAttachment);
    }

    GameObject currentOutfit; // 添加字段

    /// <summary>
    /// 更换服装，会删除旧的服装，并实例化新的服装
    /// </summary>
    /// <param name="skinnedOutfitPrefab"></param>
    public void ChangeOutfit(GameObject skinnedOutfitPrefab)
    {
        if (skinnedOutfitPrefab == null)
        {
            Debug.LogWarning("Outfit prefab is not assigned");
            return;
        }

        if (baseBonesRoot == null)
        {
            Debug.LogWarning("Base bones root is not assigned");
            return;
        }

        // 重置骨骼映射，移除之前的额外骨骼
        ResetBoneMap();

        if (currentOutfit != null)
        {
            Destroy(currentOutfit);
        }

        currentOutfit = Instantiate(skinnedOutfitPrefab, transform);

        // 处理所有 SkinnedMeshRenderer（支持多个部位）
        SkinnedMeshRenderer[] renderers = currentOutfit.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (renderers != null && renderers.Length > 0)
        {
            foreach (var renderer in renderers)
            {
                RebindBonesWithExtraBonesCompatible(renderer);
            }
        }
        else
        {
            Debug.LogWarning($"No SkinnedMeshRenderer found in {skinnedOutfitPrefab.name}");
        }

        RemoveOldBones(currentOutfit.transform);
    }

    /// <summary>
    /// 移除旧骨骼
    /// </summary>
    void RemoveOldBones(Transform targetTransform)
    {
        if (targetTransform == null || baseBonesRoot == null) return;

        // 找到与 baseBonesRoot 同名的 transform
        Transform oldRootBone = targetTransform.Find(baseBonesRoot.name);
        if (oldRootBone != null)
        {
            Destroy(oldRootBone.gameObject);
        }
    }


    /// <summary>
    /// 重新绑定骨骼，兼容额外骨骼（如尾巴等）
    /// </summary>
    /// <param name="skinnedMeshRenderer"></param>
    void RebindBonesWithExtraBonesCompatible(SkinnedMeshRenderer skinnedMeshRenderer)
    {
        if (skinnedMeshRenderer == null || baseBonesRoot == null) return;

        Transform[] newBones = new Transform[skinnedMeshRenderer.bones.Length];
        Transform rootBone = skinnedMeshRenderer.rootBone;

        for (int i = 0; i < skinnedMeshRenderer.bones.Length; i++)
        {
            Transform sourceBone = skinnedMeshRenderer.bones[i];
            if (sourceBone == null) continue;

            if (baseBonesMap.ContainsKey(sourceBone.name))
            {
                // 标准骨骼：使用基准骨骼
                newBones[i] = baseBonesMap[sourceBone.name];
            }
            else
            {
                // 额外骨骼（如尾巴）：保留原骨骼，避免变形
                // 将额外骨骼添加到基准骨骼映射中
                baseBonesMap[sourceBone.name] = sourceBone;

                // 检查并设置父节点关系（添加空检查）
                if (sourceBone.parent != null && !baseBonesMap.ContainsKey(sourceBone.parent.name))
                {
                    FindAndSetParentInBaseBonesMap(sourceBone);
                }
                else if (sourceBone.parent != null && baseBonesMap.ContainsKey(sourceBone.parent.name))
                {
                    // 父节点已在 map 中，直接设置
                    sourceBone.SetParent(baseBonesMap[sourceBone.parent.name], false);
                }

                newBones[i] = sourceBone;
            }
        }

        skinnedMeshRenderer.bones = newBones;

        // 设置根骨骼
        if (rootBone != null && baseBonesMap.ContainsKey(rootBone.name))
        {
            skinnedMeshRenderer.rootBone = baseBonesMap[rootBone.name];
        }
        else
        {
            skinnedMeshRenderer.rootBone = baseBonesRoot;
        }
    }


    /// <summary>
    /// 在基准骨骼中，找到当前骨骼的父节点，并设置当前骨骼的父节点
    /// </summary>
    /// <param name="bone"></param>
    void FindAndSetParentInBaseBonesMap(Transform bone)
    {
        Transform parent = bone.parent;
        if (parent == null) return;

        // 如果父节点不在 map 中，递归处理父节点
        if (!baseBonesMap.ContainsKey(parent.name))
        {
            FindAndSetParentInBaseBonesMap(parent);
            // 递归返回后，将父节点添加到 map
            baseBonesMap[parent.name] = parent;
        }

        // 设置当前骨骼的父节点
        bone.SetParent(baseBonesMap[parent.name], false);
    }

}
