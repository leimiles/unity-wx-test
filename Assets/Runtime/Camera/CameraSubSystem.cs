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
    public bool IsReady => _cameraService != null && _cameraService.HasMainCamera; // 修复：添加 null 检查
    ICameraService _cameraService;
    bool _installed = false; // 修复：显式初始化
    public bool IsInstalled => _installed;

    public void Install(IGameServices services)
    {
        if (_installed) return;
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        if (_cameraService == null)
        {
            throw new InvalidOperationException("CameraService not initialized. Call InitializeAsync first."); // 修复：更清晰的错误信息
        }
        services.Register<ICameraService>(_cameraService);
        _installed = true;
    }

    public UniTask InitializeAsync(IProgress<float> progress)
    {
        _cameraService = new CameraService();
        GameObject.DontDestroyOnLoad(_cameraService.MainCamera);
        progress?.Report(1.0f);
        return UniTask.CompletedTask;
    }

    public void Dispose()
    {
        // 如果需要清理，在这里实现
        // 如果 CameraService 实现了 IDisposable，应该在这里调用
    }
}
