using UnityEngine;

/// <summary>
/// 模块化角色装备管理系统
/// 职责：管理装备的更换、移除等装备相关逻辑
/// </summary>
public class ModularEquipmentSystem
{
    private ModularBoneSystem boneSystem;

    /// <summary>
    /// 构造函数 - 使用依赖注入模式
    /// </summary>
    /// <param name="modularBoneSystem">骨骼管理系统实例</param>
    public ModularEquipmentSystem(ModularBoneSystem modularBoneSystem)
    {
        if (modularBoneSystem == null)
        {
            throw new System.ArgumentNullException(nameof(modularBoneSystem), "ModularBoneSystem cannot be null");
        }
        boneSystem = modularBoneSystem;
    }

    /// <summary>
    /// 更换刚体部位（如武器、配饰等）
    /// </summary>
    /// <param name="modularChar">模块化角色数据</param>
    /// <param name="partType">部位类型</param>
    /// <param name="rigidPrefab">刚体预制体</param>
    /// <param name="attachmentPoint">挂载点（如果为 null，使用默认的 RigidAttachment）</param>
    /// <returns>是否成功更换</returns>
    public bool ChangeRigidPart(IModularChar modularChar, ModularPartType partType, GameObject rigidPrefab, Transform attachmentPoint = null)
    {
        if (modularChar == null)
        {
            Debug.LogWarning("[ModularEquipmentSystem] ModularChar is null");
            return false;
        }

        if (rigidPrefab == null)
        {
            Debug.LogWarning("[ModularEquipmentSystem] Rigid prefab is null");
            return false;
        }

        // 确定挂载点
        Transform targetAttachment = attachmentPoint ?? modularChar.RigidAttachment;
        if (targetAttachment == null)
        {
            Debug.LogWarning("[ModularEquipmentSystem] Attachment point is null");
            return false;
        }

        // 移除旧部位
        RemovePart(modularChar, partType);

        // 实例化新部位
        GameObject newPart = GameObject.Instantiate(rigidPrefab, targetAttachment, true);
        modularChar.InternalSetPart(partType, newPart);

        return true;
    }

    /// <summary>
    /// 更换蒙皮部位（如服装、头发等）
    /// </summary>
    /// <param name="modularChar">模块化角色数据</param>
    /// <param name="partType">部位类型</param>
    /// <param name="skinnedPartPrefab">蒙皮预制体</param>
    /// <param name="resetBoneMap">是否重置骨骼映射（移除额外骨骼）</param>
    /// <returns>是否成功更换</returns>
    public bool ChangeSkinnedPart(IModularChar modularChar, ModularPartType partType, GameObject skinnedPartPrefab, bool resetBoneMap = true)
    {
        if (modularChar == null)
        {
            Debug.LogWarning("[ModularEquipmentSystem] ModularChar is null");
            return false;
        }

        if (skinnedPartPrefab == null)
        {
            Debug.LogWarning("[ModularEquipmentSystem] Skinned part prefab is null");
            return false;
        }

        if (!modularChar.IsBoneMapVerified)
        {
            Debug.LogWarning("[ModularEquipmentSystem] Bone map not verified, verifying now...");
            if (!boneSystem.VerifyBoneMap(modularChar))
            {
                Debug.LogError("[ModularEquipmentSystem] Failed to verify bone map");
                return false;
            }
        }

        // 重置骨骼映射（移除之前的额外骨骼）
        if (resetBoneMap)
        {
            boneSystem.ResetBoneMap(modularChar);
        }

        // 移除旧部位
        RemovePart(modularChar, partType);

        // 实例化新部位
        Transform charTransform = (modularChar as MonoBehaviour)?.transform;
        if (charTransform == null)
        {
            Debug.LogError("[ModularEquipmentSystem] Cannot get Transform from ModularChar");
            return false;
        }

        GameObject newPart = GameObject.Instantiate(skinnedPartPrefab, charTransform);
        modularChar.InternalSetPart(partType, newPart);

        // 处理所有 SkinnedMeshRenderer（支持多个部位）
        SkinnedMeshRenderer[] renderers = newPart.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (renderers != null && renderers.Length > 0)
        {
            foreach (var renderer in renderers)
            {
                if (!boneSystem.RebindBones(renderer, modularChar))
                {
                    Debug.LogWarning($"[ModularEquipmentSystem] Failed to rebind bones for renderer: {renderer.name}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[ModularEquipmentSystem] No SkinnedMeshRenderer found in {skinnedPartPrefab.name}");
        }

        // 移除旧骨骼
        boneSystem.RemoveOldBones(newPart.transform, modularChar);

        return true;
    }

    /// <summary>
    /// 移除指定部位
    /// </summary>
    /// <param name="modularChar">模块化角色数据</param>
    /// <param name="partType">部位类型</param>
    /// <returns>是否成功移除</returns>
    public bool RemovePart(IModularChar modularChar, ModularPartType partType)
    {
        if (modularChar == null)
        {
            return false;
        }

        if (modularChar.TryGetPart(partType, out GameObject part))
        {
            if (part != null)
            {
                Object.Destroy(part);
            }
            modularChar.InternalRemovePart(partType);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 移除所有部位
    /// </summary>
    /// <param name="modularChar">模块化角色数据</param>
    public void RemoveAllParts(IModularChar modularChar)
    {
        if (modularChar == null)
        {
            return;
        }

        var partsToRemove = new System.Collections.Generic.List<ModularPartType>(modularChar.GetAllEquippedParts());
        foreach (var partType in partsToRemove)
        {
            RemovePart(modularChar, partType);
        }
    }

    /// <summary>
    /// 批量更换部位（用于一次性更换多个部位，如套装）
    /// </summary>
    /// <param name="modularChar">模块化角色数据</param>
    /// <param name="parts">部位配置字典（部位类型 -> 预制体）</param>
    /// <param name="isSkinned">是否为蒙皮部位（true=蒙皮，false=刚体）</param>
    /// <returns>成功更换的数量</returns>
    public int ChangePartsBatch(IModularChar modularChar, System.Collections.Generic.Dictionary<ModularPartType, GameObject> parts, bool isSkinned = true)
    {
        if (modularChar == null || parts == null || parts.Count == 0)
        {
            return 0;
        }

        int successCount = 0;

        foreach (var kvp in parts)
        {
            bool success = isSkinned
                ? ChangeSkinnedPart(modularChar, kvp.Key, kvp.Value, resetBoneMap: false) // 批量更换时，只在最后一个重置
                : ChangeRigidPart(modularChar, kvp.Key, kvp.Value);

            if (success)
            {
                successCount++;
            }
        }

        // 批量更换完成后，重置一次骨骼映射
        if (isSkinned && successCount > 0)
        {
            boneSystem.ResetBoneMap(modularChar);
        }

        return successCount;
    }
}

