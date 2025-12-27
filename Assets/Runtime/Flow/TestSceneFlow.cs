using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
public class TestSceneFlow : IGameFlow
{
    readonly IGameServices _services;
    public TestSceneFlow(IGameServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }
    public async UniTask RunAsync(CancellationToken ct)
    {
        // 加载场景
        var sceneService = _services.Get<IGameSceneService>();
        await sceneService.LoadSceneAsync("Main").AttachExternalCancellation(ct);

        // 切流
        //EventBus<RequestFlowSwitchEvent>.Raise(new RequestFlowSwitchEvent(FlowID.TestUI));

        // 设置世界
        var gameWorldService = _services.Get<IGameWorldService>();
        gameWorldService.SetCurrentWorld();

        // 获取相机
        var cameraService = _services.Get<ICameraService>();

        // 获取控制器
        var controlService = _services.Get<IControlService>();

        // 设置相机控制器
        controlService.CameraControlRig = new JustEntryCameraControlRig(cameraService);
        controlService.SwitchCameraControlRig(controlService.CameraControlRig);

        //controlService.SwitchCameraControlRig(cameraControlRig);
    }
}
