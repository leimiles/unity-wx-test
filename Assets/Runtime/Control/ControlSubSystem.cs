using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
public class ControlSubSystem : ISubSystem
{
    public string Name => "ControlSubSystem";
    public int Priority => 5;
    public bool IsRequired => true;
    public bool IsReady => _isInitialized;
    bool _isInitialized;
    public bool IsInstalled => _installed;
    bool _installed = false;
    public void Install(IGameServices services)
    {
        if (_installed) return;
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        _installed = true;
    }
    public UniTask InitializeAsync(IProgress<float> progress)
    {
        _isInitialized = true;
        progress?.Report(1.0f);
        return UniTask.CompletedTask;
    }
    public void Dispose()
    {
        // 如果需要清理，在这里实现
    }
}
