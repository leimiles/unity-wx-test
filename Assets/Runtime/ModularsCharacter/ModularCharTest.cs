using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ModularCharMonoRef))]
public class ModularCharTest : MonoBehaviour
{
    [SerializeField] ModularCharMonoRef modularCharMonoRef;
    ModularBoneSystem modularBoneSystem;
    ModularEquipmentSystem modularEquipmentSystem;

    #region Test
    [SerializeField] GameObject rigidPrefabTest1;
    [SerializeField] GameObject rigidPrefabTest2;
    [SerializeField] GameObject skinnedOutfitPrefabTest1;
    [SerializeField] GameObject skinnedOutfitPrefabTest2;
    [SerializeField] GameObject skinnedOutfitPrefabTest3;
    #endregion

    /// <summary>
    /// 测试更换装备
    /// </summary>
    void Start()
    {
        modularCharMonoRef = GetComponent<ModularCharMonoRef>();
        if (modularCharMonoRef == null)
        {
            Debug.LogError("ModularCharMonoRef is not found");
            return;
        }
        modularBoneSystem = new ModularBoneSystem();
        modularEquipmentSystem = new ModularEquipmentSystem(modularBoneSystem);


        modularEquipmentSystem.ChangeRigidPart(modularCharMonoRef, ModularPartType.Rigid, rigidPrefabTest1);

        modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Hair, skinnedOutfitPrefabTest1);
        modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.UpperBody, skinnedOutfitPrefabTest2);
        modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Outfit, skinnedOutfitPrefabTest3);

#if UNITY_EDITOR
        var boneRenderer = modularCharMonoRef.GetBoneRenderer();
        if (boneRenderer != null)
        {
            Debug.Log("Reset bone renderer");
            boneRenderer.ClearBones();
            boneRenderer.transforms = modularCharMonoRef.BaseBonesRoot.GetComponentsInChildren<Transform>();
            boneRenderer.Invalidate();
        }
#endif
    }
}
