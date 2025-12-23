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
        var sceneService = _services.Get<IGameSceneService>();
        await sceneService.LoadSceneAsync("Main").AttachExternalCancellation(ct);

        // 重置游戏世界
        EventBus<GameWorldEnterEvent>.Raise(new GameWorldEnterEvent { gameWorldName = "DemoWorld" });
        EventBus<RequestFlowSwitchEvent>.Raise(new RequestFlowSwitchEvent(FlowID.TestUI));
    }
}
