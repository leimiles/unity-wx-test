using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[DisallowMultipleComponent]
public class ModularCharSystem : MonoBehaviour
{
    [SerializeField] Transform rootBone;
    [SerializeField] Transform headAttachment;
    GameObject hairPrefab;
    [SerializeField] GameObject uppberBodyPrefab;
    Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();

    void Start()
    {
        VerifyBoneMap();
        ChangeUpperBody();
    }


    void VerifyBoneMap()
    {
        if (rootBone != null)
        {
            foreach (var bone in rootBone.GetComponentsInChildren<Transform>())
            {
                boneMap[bone.name] = bone;
            }
        }
    }

    public void ChangeHair()
    {
        if (hairPrefab != null && !hairPrefab.activeInHierarchy)
        {
            hairPrefab = Instantiate(hairPrefab, headAttachment);
            hairPrefab.transform.GetChild(0).transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    public void ChangeUpperBody()
    {
        if (uppberBodyPrefab != null && !uppberBodyPrefab.activeInHierarchy)
        {
            uppberBodyPrefab = Instantiate(uppberBodyPrefab, transform);
            uppberBodyPrefab.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            RebindBonesWithExtraBonesCompatible(uppberBodyPrefab.GetComponentInChildren<SkinnedMeshRenderer>());
            RemoveOldBones(uppberBodyPrefab.transform.GetChild(0));
        }

    }

    void RemoveOldBones(Transform oldBone)
    {
        if (oldBone.name == rootBone.name)
        {
            Debug.Log($"root bone name: <color=red>{rootBone.name}</color>");
            Debug.Log($"old bone name: <color=green>{oldBone.name}</color>");
        }

    }

    void RebindBones(SkinnedMeshRenderer skinnedMeshRenderer)
    {
        // 用红色字体输出每个 skinnedMeshRenderer.bones 的名字，方便调试
        //Debug.Log($"Rebinding bones for <color=red>{skinnedMeshRenderer.name}</color>");

        Transform[] newBones = new Transform[skinnedMeshRenderer.bones.Length];
        for (int i = 0; i < skinnedMeshRenderer.bones.Length; i++)
        {
            Transform newbone = skinnedMeshRenderer.bones[i];

            if (boneMap.ContainsKey(newbone.name))
            {
                newBones[i] = boneMap[newbone.name];
            }
            else
            {
                Debug.LogWarning("new bone not match");
            }
        }

        skinnedMeshRenderer.bones = newBones;
        skinnedMeshRenderer.rootBone = rootBone;
    }

    void RebindBonesWithExtraBonesCompatible(SkinnedMeshRenderer skinnedMeshRenderer)
    {
        Transform[] newBones = new Transform[skinnedMeshRenderer.bones.Length];

        for (int i = 0; i < skinnedMeshRenderer.bones.Length; i++)
        {
            Transform sourceBone = skinnedMeshRenderer.bones[i];

            if (boneMap.ContainsKey(sourceBone.name))
            {
                // 标准骨骼：使用基准骨骼
                newBones[i] = boneMap[sourceBone.name];

                Debug.Log($"Standard bone '{sourceBone.name}' found in base skeleton, using base bone");
            }
            else
            {
                // 额外骨骼（如尾巴）：保留原骨骼，避免变形
                newBones[i] = sourceBone;
                Debug.Log($"Extra bone '{sourceBone.name}' not found in base skeleton, using original bone");
                SetParentNodeInBoneMap(sourceBone);
            }
        }

        skinnedMeshRenderer.bones = newBones;
        skinnedMeshRenderer.rootBone = rootBone;
    }

    void SetParentNodeInBoneMap(Transform bone)
    {
        Debug.Log($"SetParentNodeInBoneMap: {bone.parent.name}");
    }

}
