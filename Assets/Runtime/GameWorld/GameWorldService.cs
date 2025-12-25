using UnityEngine;
using System;
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
    private const string GameWorldTag = "GameWorld";
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
        try
        {
            SetCurrentWorld();

            // 验证设置是否成功
            if (_currentWorld == null)
            {
                throw new InvalidOperationException("SetCurrentWorld completed but _currentWorld is still null");
            }

            Debug.Log($"[GameWorldService] enter game world: {e.gameWorldName} -> {_currentWorld.Name}");
        }
        catch (InvalidOperationException ex)
        {
            Debug.LogError($"[GameWorldService] Failed to set current world for '{e.gameWorldName}': {ex.Message}");

            // 确保状态一致
            _currentWorld = null;

            // 可以考虑触发错误恢复流程
            // 例如：重新加载场景、显示错误提示等
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameWorldService] Unexpected error in OnGameWorldEnter for '{e.gameWorldName}': {ex.Message}");
            Debug.LogException(ex);
            _currentWorld = null;
        }
    }

    /// <summary>
    /// 设置当前游戏世界，非常严格，必须只有一个GameWorld对象，并且必须实现IGameWorld接口
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    void SetCurrentWorld()
    {
        var gos = GameObject.FindGameObjectsWithTag(GameWorldTag);

        if (gos == null || gos.Length == 0)
            throw new InvalidOperationException($"[GameWorldService] No GameWorld found (tag='{GameWorldTag}').");

        if (gos.Length > 1)
        {
            var names = "";
            for (int i = 0; i < gos.Length; i++)
            {
                if (i > 0) names += ", ";
                names += gos[i].name;
            }

            throw new InvalidOperationException(
                $"[GameWorldService] Multiple GameWorld objects found (tag='{GameWorldTag}'): {gos.Length}. Objects: {names}");
        }


        var go = gos[0];

        if (!go.TryGetComponent<IGameWorld>(out var world) || world == null)
            throw new InvalidOperationException(
                $"[GameWorldService] GameObject '{go.name}' has tag '{GameWorldTag}' but does not implement IGameWorld.");

        _currentWorld = world;

        Debug.Log($"[GameWorldService] set current game world: '{_currentWorld.Name}' (GO='{go.name}')");
    }

    public void Dispose()
    {
        _gameWorldEnterBinding?.Dispose();
        _gameWorldEnterBinding = null;
    }
}

