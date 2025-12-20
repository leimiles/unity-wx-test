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
    [SerializeField] GameObject[] skinnedOutfitPrefabTests;
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
        // modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Outfit, skinnedOutfitPrefabTest1);
        // modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Gloves, skinnedOutfitPrefabTest2);
        // modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Shoes, skinnedOutfitPrefabTest3);
        if (skinnedOutfitPrefabTests != null && skinnedOutfitPrefabTests.Length > 0)
        {
            if (skinnedOutfitPrefabTests.Length == 1)
            {
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Outfit, skinnedOutfitPrefabTests[0]);
            }
            else if (skinnedOutfitPrefabTests.Length == 2)
            {
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.UpperBody, skinnedOutfitPrefabTests[0]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.LowerBody, skinnedOutfitPrefabTests[1]);
            }
            else if (skinnedOutfitPrefabTests.Length == 3)
            {
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Outfit, skinnedOutfitPrefabTests[0]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Gloves, skinnedOutfitPrefabTests[1]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Shoes, skinnedOutfitPrefabTests[2]);
            }
            else if (skinnedOutfitPrefabTests.Length == 4)
            {
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.UpperBody, skinnedOutfitPrefabTests[0]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.LowerBody, skinnedOutfitPrefabTests[1]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Gloves, skinnedOutfitPrefabTests[2]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Shoes, skinnedOutfitPrefabTests[3]);
            }
            else if (skinnedOutfitPrefabTests.Length == 5)
            {
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Outfit, skinnedOutfitPrefabTests[0]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.UpperBody, skinnedOutfitPrefabTests[1]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.LowerBody, skinnedOutfitPrefabTests[2]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Gloves, skinnedOutfitPrefabTests[3]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Shoes, skinnedOutfitPrefabTests[4]);
            }
            else if (skinnedOutfitPrefabTests.Length == 6)
            {
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Outfit, skinnedOutfitPrefabTests[0]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.UpperBody, skinnedOutfitPrefabTests[1]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.LowerBody, skinnedOutfitPrefabTests[2]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Gloves, skinnedOutfitPrefabTests[3]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Shoes, skinnedOutfitPrefabTests[4]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Backpack, skinnedOutfitPrefabTests[5]);
            }
            else if (skinnedOutfitPrefabTests.Length == 7)
            {
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Outfit, skinnedOutfitPrefabTests[0]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.UpperBody, skinnedOutfitPrefabTests[1]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.LowerBody, skinnedOutfitPrefabTests[2]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Gloves, skinnedOutfitPrefabTests[3]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Shoes, skinnedOutfitPrefabTests[4]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Backpack, skinnedOutfitPrefabTests[5]);
                modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Accessory, skinnedOutfitPrefabTests[6]);
            }
            else
            {
                Debug.LogError("Skinned outfit prefab tests length is not supported");
            }
        }

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
