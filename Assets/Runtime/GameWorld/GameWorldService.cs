using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
public interface IGameWorldService
{
    bool HasWorld { get; }
    IGameWorld CurrentWorld { get; }
    // 如果重制，需要序列执行这些步骤
    UniTask ResetAsync();
    void SetCurrentWorld();
}

public class GameWorldService : IGameWorldService
{
    private const string GameWorldTag = "GameWorld";
    public bool HasWorld => _currentWorld != null;
    IGameWorld _currentWorld;
    public IGameWorld CurrentWorld => _currentWorld;

    public UniTask ResetAsync()
    {
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// 设置当前游戏世界，非常严格，必须只有一个GameWorld对象，并且必须实现IGameWorld接口
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void SetCurrentWorld()
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

        if (!go.TryGetComponent<IGameWorld>(out var world))
            throw new InvalidOperationException(
                $"[GameWorldService] GameObject '{go.name}' has tag '{GameWorldTag}' but does not implement IGameWorld.");

        _currentWorld = world;

        Debug.Log($"[GameWorldService] set current game world: '{_currentWorld.Name}' (GO='{go.name}')");
    }

}

