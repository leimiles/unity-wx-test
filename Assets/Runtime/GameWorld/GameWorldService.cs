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
    UniTask ResetAsync();
}

public class GameWorldService : IGameWorldService
{
    public bool HasWorld => _currentWorld != null;
    IGameWorld _currentWorld;
    public IGameWorld CurrentWorld => _currentWorld;
    EventBinding<GameWorldEnterEvent> _gameWorldEnterBinding;

    public UniTask ResetAsync()
    {
        return UniTask.CompletedTask;
    }

    public GameWorldService()
    {
        _gameWorldEnterBinding = new EventBinding<GameWorldEnterEvent>(OnGameWorldEnter);
        EventBus<GameWorldEnterEvent>.Register(_gameWorldEnterBinding);
    }

    void OnGameWorldEnter(GameWorldEnterEvent e)
    {
        Debug.Log("[GameWorldService] enter game world");
    }

    public void Dispose()
    {
        EventBus<GameWorldEnterEvent>.Deregister(_gameWorldEnterBinding);
        _gameWorldEnterBinding = null;
    }
}

