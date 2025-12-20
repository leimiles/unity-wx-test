using System.Collections.Generic;
using UnityEngine;
using MilesUtils;
using System;

public class GameManager : PersistentSingleton<GameManager>
{
    IReadOnlyList<ISubSystem> _subSystems;
    protected override void Awake()
    {
        base.Awake();
    }

    void OnDestroy()
    {
        if (_subSystems == null) return;
        // 通知所有子系统释放所有资源
        foreach (var subSystem in _subSystems)
        {
            try
            {
                subSystem.Dispose();
                Debug.Log($"SubSystem {subSystem.Name} disposed");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to dispose SubSystem {subSystem.Name}: {e.Message}");
            }
        }
    }

    public void AttachSubSystems(IReadOnlyList<ISubSystem> subSystems)
    {
        if (subSystems == null) return;
        _subSystems = new List<ISubSystem>(subSystems);
    }

}
