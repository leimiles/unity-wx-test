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

        // 控制器就绪
        var controlService = _services.Get<IControlService>();
        controlService.OnWorldReady(gameWorldService.CurrentWorld);
    }
}
