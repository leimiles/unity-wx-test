using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine;

public class EntryFlow : IGameFlow
{
    readonly IGameServices _services;
    public EntryFlow(IGameServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }
    public async UniTask RunAsync(CancellationToken ct)
    {
        var sceneService = _services.Get<IGameSceneService>();
        await sceneService.LoadSceneAsync("Main").AttachExternalCancellation(ct);

        // 重置游戏世界
        EventBus<GameWorldEnterEvent>.Raise(new GameWorldEnterEvent(ct));
    }
}
