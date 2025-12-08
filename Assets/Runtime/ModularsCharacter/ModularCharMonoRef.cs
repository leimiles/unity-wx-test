using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.Animations.Rigging;
#endif
/// <summary>
/// 模块化角色 Mono 组件 - 纯数据类，不包含业务逻辑
/// 参考实现：展示如何实现 IModularChar 接口
/// </summary>
[DisallowMultipleComponent]
public class ModularCharMonoRef : MonoBehaviour, IModularChar
{
    // ========== 序列化字段 ==========
    [SerializeField] private Transform baseBonesRoot;
    [SerializeField] private Transform rigidAttachment;

#if UNITY_EDITOR
    [SerializeField] private BoneRenderer boneRenderer;
#endif

    // ========== 私有数据存储 ==========
    private Dictionary<string, Transform> baseBonesMap = new Dictionary<string, Transform>();
    private Dictionary<string, Transform> baseBonesOriginal = new Dictionary<string, Transform>();
    private Dictionary<ModularPartType, GameObject> onBodyParts = new Dictionary<ModularPartType, GameObject>();
    private bool isBoneMapVerified = false;

    // ========== IModularChar 接口实现 - 公共访问 ==========
    public bool IsBoneMapVerified => isBoneMapVerified;
    public Transform BaseBonesRoot => baseBonesRoot;
    public Transform RigidAttachment => rigidAttachment;

    public bool TryGetBone(string boneName, out Transform bone)
    {
        return baseBonesMap.TryGetValue(boneName, out bone);
    }

    public bool HasBone(string boneName)
    {
        return baseBonesMap.ContainsKey(boneName);
    }

    public IEnumerable<string> GetAllBoneNames()
    {
        return baseBonesMap.Keys;
    }

    public bool TryGetPart(ModularPartType partType, out GameObject part)
    {
        return onBodyParts.TryGetValue(partType, out part);
    }

    public bool HasPart(ModularPartType partType)
    {
        return onBodyParts.ContainsKey(partType) && onBodyParts[partType] != null;
    }

    public IEnumerable<ModularPartType> GetAllEquippedParts()
    {
        return onBodyParts.Keys;
    }

    // ========== IModularChar 接口实现 - 内部访问 ==========
    Dictionary<string, Transform> IModularChar.InternalBonesMap => baseBonesMap;
    Dictionary<string, Transform> IModularChar.InternalOriginalBonesMap => baseBonesOriginal;
    Dictionary<ModularPartType, GameObject> IModularChar.InternalPartsMap => onBodyParts;

    void IModularChar.SetBoneMapVerified(bool verified)
    {
        isBoneMapVerified = verified;
    }

    void IModularChar.InternalAddBone(string boneName, Transform bone)
    {
        if (bone != null)
        {
            baseBonesMap[boneName] = bone;
        }
    }

    bool IModularChar.InternalRemoveBone(string boneName)
    {
        return baseBonesMap.Remove(boneName);
    }

    void IModularChar.InternalSetPart(ModularPartType partType, GameObject part)
    {
        onBodyParts[partType] = part;
    }

    bool IModularChar.InternalRemovePart(ModularPartType partType)
    {
        return onBodyParts.Remove(partType);
    }

    // ========== Unity 生命周期 ==========
    private void OnDestroy()
    {
        // 清理资源
        baseBonesMap.Clear();
        baseBonesOriginal.Clear();
        onBodyParts.Clear();
    }

#if UNITY_EDITOR
    public BoneRenderer GetBoneRenderer()
    {
        return boneRenderer;
    }
#endif
}

