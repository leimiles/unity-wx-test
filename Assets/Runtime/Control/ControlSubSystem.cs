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
    public bool IsReady => _controlService != null;
    public bool IsInstalled => _installed;
    IControlService _controlService;
    bool _installed = false;
    readonly IGameServices _gameServices;
    public ControlSubSystem(IGameServices gameServices)
    {
        if (gameServices == null)
        {
            throw new ArgumentNullException(nameof(gameServices));
        }
        _gameServices = gameServices ?? throw new ArgumentNullException(nameof(gameServices));
    }
    public UniTask InitializeAsync(IProgress<float> progress)
    {
        var cameraService = _gameServices.Get<ICameraService>();
        if (cameraService == null)
        {
            throw new InvalidOperationException("CameraService is not initialized before InitializeAsync");
        }
        _controlService = new ControlService(cameraService);
        progress?.Report(1f);
        return UniTask.CompletedTask;
    }

    public void Install(IGameServices services)
    {
        if (_installed) return;
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        services.Register<IControlService>(_controlService);
        _installed = true;
    }
    public void Dispose()
    {
        // 如果需要清理，在这里实现
    }
}

