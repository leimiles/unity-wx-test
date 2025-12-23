using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

public class GameWorldSubSystem : ISubSystem
{
    readonly IGameWorldService _gameWorldService;
    public string Name => "GameWorldSubSystem";
    public int Priority => 3;
    public bool IsRequired => true;
    public bool IsInitialized => _isInitialized;
    bool _isInitialized;
    public GameWorldSubSystem(IGameWorldService gameWorldService)
    {
        _gameWorldService = gameWorldService ?? throw new ArgumentNullException(nameof(gameWorldService));
    }
    public UniTask InitializeAsync(IProgress<float> progress)
    {
        // 不需要初始化，直接设置为已初始化
        _isInitialized = true;
        progress?.Report(1.0f);
        return UniTask.CompletedTask;
    }
    public void Dispose()
    {
        // 如果需要清理，在这里实现
    }
}
