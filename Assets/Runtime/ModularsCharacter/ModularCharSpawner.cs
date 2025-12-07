using UnityEngine;
using MilesUtils;

/// <summary>
/// 模块角色生成器，仅负责管理角色（ModularCharSystem）的生成，销毁
/// </summary>
public class ModularCharSpawner : Singleton<ModularCharSpawner>
{
    ModularCharMonoRef modularCharMonoRefPrefab;
    static int instanceCount = 0;

    public void Spawn()
    {
        instanceCount++;
        Debug.Log($"Spawn: {instanceCount}");
    }

    public void Despawn()
    {
        instanceCount--;
        Debug.Log($"Despawn: {instanceCount}");
    }

}