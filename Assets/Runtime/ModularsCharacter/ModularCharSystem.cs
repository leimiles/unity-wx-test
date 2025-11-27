using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[DisallowMultipleComponent]
public class ModularCharSystem : MonoBehaviour
{
    [SerializeField] Transform rootBone;
    [SerializeField] Transform headAttachment;
    GameObject hairPrefab;
    GameObject uppberBodyPrefab;
    Dictionary<string, Transform> boneMap = new Dictionary<string, Transform>();

    void Awake()
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
        if (hairPrefab == null)
        {
            hairPrefab = Resources.Load<GameObject>("mdl_hair01");
        }

        if (hairPrefab != null && !hairPrefab.activeInHierarchy)
        {
            hairPrefab = Instantiate(hairPrefab, headAttachment);
            hairPrefab.transform.GetChild(0).transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }

    public void ChangeUpperBody()
    {
        if (uppberBodyPrefab == null)
        {
            uppberBodyPrefab = Resources.Load<GameObject>("mdl_upperbody01");
        }
        if (uppberBodyPrefab != null && !uppberBodyPrefab.activeInHierarchy)
        {
            uppberBodyPrefab = Instantiate(uppberBodyPrefab, transform);
            uppberBodyPrefab.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            RebindBones(uppberBodyPrefab.GetComponentInChildren<SkinnedMeshRenderer>());
            RemoveOldBones(uppberBodyPrefab.transform.GetChild(0));
        }

    }

    void RemoveOldBones(Transform oldBone)
    {
        if (oldBone.name == "Girl_Bip001")
        {
            Destroy(oldBone.gameObject);
        }
    }

    void RebindBones(SkinnedMeshRenderer skinnedMeshRenderer)
    {
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
}
