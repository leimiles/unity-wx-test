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
    [SerializeField] bool rigidPartStaysWorldPosition = false;
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
        modularEquipmentSystem.ChangeRigidPart(modularCharMonoRef, ModularPartType.Rigid, rigidPrefabTest1, rigidPartStaysWorldPosition);
        if (skinnedOutfitPrefabTests != null && skinnedOutfitPrefabTests.Length > 0)
        {
            switch (skinnedOutfitPrefabTests.Length)
            {
                case 1:
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Outfit, skinnedOutfitPrefabTests[0]);
                    break;
                case 2:
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.UpperBody, skinnedOutfitPrefabTests[0]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.LowerBody, skinnedOutfitPrefabTests[1]);
                    break;
                case 3:
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Outfit, skinnedOutfitPrefabTests[0]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Gloves, skinnedOutfitPrefabTests[1]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Shoes, skinnedOutfitPrefabTests[2]);
                    break;
                case 4:
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.UpperBody, skinnedOutfitPrefabTests[0]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.LowerBody, skinnedOutfitPrefabTests[1]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Gloves, skinnedOutfitPrefabTests[2]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Shoes, skinnedOutfitPrefabTests[3]);
                    break;
                case 5:
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Outfit, skinnedOutfitPrefabTests[0]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.UpperBody, skinnedOutfitPrefabTests[1]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.LowerBody, skinnedOutfitPrefabTests[2]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Gloves, skinnedOutfitPrefabTests[3]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Shoes, skinnedOutfitPrefabTests[4]);
                    break;
                case 6:
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Outfit, skinnedOutfitPrefabTests[0]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.UpperBody, skinnedOutfitPrefabTests[1]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.LowerBody, skinnedOutfitPrefabTests[2]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Gloves, skinnedOutfitPrefabTests[3]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Shoes, skinnedOutfitPrefabTests[4]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Backpack, skinnedOutfitPrefabTests[5]);
                    break;
                case 7:
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Outfit, skinnedOutfitPrefabTests[0]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.UpperBody, skinnedOutfitPrefabTests[1]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.LowerBody, skinnedOutfitPrefabTests[2]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Gloves, skinnedOutfitPrefabTests[3]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Shoes, skinnedOutfitPrefabTests[4]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Backpack, skinnedOutfitPrefabTests[5]);
                    modularEquipmentSystem.ChangeSkinnedPart(modularCharMonoRef, ModularPartType.Accessory, skinnedOutfitPrefabTests[6]);
                    break;
                default:
                    Debug.LogError("Skinned outfit prefab tests length is not supported");
                    break;
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
