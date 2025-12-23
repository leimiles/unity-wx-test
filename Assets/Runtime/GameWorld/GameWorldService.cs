using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
public interface IGameWorldService : IDisposable
{
    bool HasWorld { get; }
    IGameWorld CurrentWorld { get; }
    // 如果重制，需要序列执行这些步骤
    UniTask ResetAsync(CancellationToken ct);
}

public class GameWorldService : IGameWorldService
{
    public bool HasWorld => _currentWorld != null;
    IGameWorld _currentWorld;
    public IGameWorld CurrentWorld => _currentWorld;
    EventBinding<GameWorldEnterEvent> GameWorldEnterBinding { get; }
    private readonly SemaphoreSlim _resetGate = new(1, 1);

    public async UniTask ResetAsync(CancellationToken ct)
    {
        await _resetGate.WaitAsync(ct);
        try
        {
            Debug.Log("[GameWorldService] reset begin");

            // 这里写你的 reset pipeline
            // Acquire world
            // Reset player
            // Reset camera

            Debug.Log("[GameWorldService] reset end");
        }
        finally
        {
            _resetGate.Release();
        }
    }

    // 各种设置

    public GameWorldService()
    {
        GameWorldEnterBinding = new EventBinding<GameWorldEnterEvent>(OnGameWorldEnter);
        EventBus<GameWorldEnterEvent>.Register(GameWorldEnterBinding);
    }

    void OnGameWorldEnter(GameWorldEnterEvent e)
    {
        Debug.Log("[GameWorldService] enter game world");
        ResetAsync(e.CancellationToken).Forget();
    }

    public void Dispose()
    {
        _resetGate.Dispose();
        EventBus<GameWorldEnterEvent>.Deregister(GameWorldEnterBinding);
    }
}

