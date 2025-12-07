using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 模块化角色数据接口 - 提供受控的数据访问
/// </summary>
public interface IModularChar
{
    // ========== 基础属性 ==========
    /// <summary>是否已验证骨骼映射</summary>
    bool IsBoneMapVerified { get; }

    /// <summary>基准骨骼根节点</summary>
    Transform BaseBonesRoot { get; }

    /// <summary>刚体部位挂载点（用于 Rigid 类型部位）</summary>
    Transform RigidAttachment { get; }

    // ========== 骨骼访问方法（受控访问） ==========
    /// <summary>尝试获取骨骼 Transform</summary>
    bool TryGetBone(string boneName, out Transform bone);

    /// <summary>检查骨骼是否存在</summary>
    bool HasBone(string boneName);

    /// <summary>获取所有骨骼名称</summary>
    IEnumerable<string> GetAllBoneNames();

    // ========== 部位访问方法（受控访问） ==========
    /// <summary>尝试获取部位 GameObject</summary>
    bool TryGetPart(ModularPartType partType, out GameObject part);

    /// <summary>检查部位是否存在</summary>
    bool HasPart(ModularPartType partType);

    /// <summary>获取所有已装备的部位类型</summary>
    IEnumerable<ModularPartType> GetAllEquippedParts();

    // ========== 内部访问（仅供 System 使用） ==========
    /// <summary>内部：获取骨骼映射字典（仅供 System 使用）</summary>
    internal Dictionary<string, Transform> InternalBonesMap { get; }

    /// <summary>内部：获取原始骨骼映射字典（仅供 System 使用）</summary>
    internal Dictionary<string, Transform> InternalOriginalBonesMap { get; }

    /// <summary>内部：获取部位映射字典（仅供 System 使用）</summary>
    internal Dictionary<ModularPartType, GameObject> InternalPartsMap { get; }

    /// <summary>内部：设置骨骼映射验证状态（仅供 System 使用）</summary>
    internal void SetBoneMapVerified(bool verified);

    /// <summary>内部：添加或更新骨骼（仅供 System 使用）</summary>
    internal void InternalAddBone(string boneName, Transform bone);

    /// <summary>内部：移除骨骼（仅供 System 使用）</summary>
    internal bool InternalRemoveBone(string boneName);

    /// <summary>内部：添加或更新部位（仅供 System 使用）</summary>
    internal void InternalSetPart(ModularPartType partType, GameObject part);

    /// <summary>内部：移除部位（仅供 System 使用）</summary>
    internal bool InternalRemovePart(ModularPartType partType);
}

