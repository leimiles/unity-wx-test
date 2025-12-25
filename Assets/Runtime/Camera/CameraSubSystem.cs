using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class CameraSubSystem : ISubSystem
{
    public string Name => "CameraSubSystem";
    public int Priority => 4;
    public bool IsRequired => true;
    public bool IsInitialized => _isInitialized;
    bool _isInitialized;
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
