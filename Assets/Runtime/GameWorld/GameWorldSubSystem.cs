using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

public class GameWorldSubSystem : ISubSystem
{
    public string Name => "GameWorldSubSystem";
    public int Priority => 3;
    public bool IsRequired => true;
    public bool IsReady => _gameWorldService != null;   // 不能以是否 has world 来判断，因为 has world 需要等待 GameWorldEnterEvent 事件触发
    public bool IsInstalled => _installed;
    bool _installed = false;
    IGameWorldService _gameWorldService;
    public void Install(IGameServices services)
    {
        if (_installed) return;
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        if (_gameWorldService == null)
        {
            throw new InvalidOperationException(
                "GameWorldService is not initialized before Install"
            );
        }
        services.Register<IGameWorldService>(_gameWorldService);
        _installed = true;
    }
    public UniTask InitializeAsync(IProgress<float> progress)
    {
        // 不需要初始化，直接设置为已初始化
        _gameWorldService = new GameWorldService();
        progress?.Report(1.0f);
        return UniTask.CompletedTask;
    }
    public void Dispose()
    {
        // 如果需要清理，在这里实现
    }
}
