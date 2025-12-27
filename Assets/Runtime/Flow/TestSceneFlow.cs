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
        try
        {
            // 加载场景
            var sceneService = _services.Get<IGameSceneService>();
            if (sceneService == null)
            {
                throw new InvalidOperationException("IGameSceneService not found");
            }

            await sceneService.LoadSceneAsync("Main").AttachExternalCancellation(ct);

            // 设置世界
            var gameWorldService = _services.Get<IGameWorldService>();
            if (gameWorldService == null)
            {
                throw new InvalidOperationException("IGameWorldService not found");
            }

            gameWorldService.SetCurrentWorld();

            if (gameWorldService.CurrentWorld == null)
            {
                throw new InvalidOperationException("CurrentWorld is null after SetCurrentWorld");
            }

            // 控制器就绪
            var controlService = _services.Get<IControlService>();
            if (controlService == null)
            {
                throw new InvalidOperationException("IControlService not found");
            }

            controlService.OnWorldReady(gameWorldService.CurrentWorld);
        }
        catch (Exception ex)
        {
            throw new Exception($"[TestSceneFlow] Failed to run scene flow: {ex.Message}", ex);  // 重新抛出，让上层处理
        }
    }
}
