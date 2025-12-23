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
        // 暂不实现
        await UniTask.CompletedTask;
    }
}
