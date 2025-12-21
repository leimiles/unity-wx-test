using System.Collections.Generic;
using UnityEngine;
using MilesUtils;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public class GameManager : PersistentSingleton<GameManager>
{
    bool _attached;
    IReadOnlyList<ISubSystem> _subSystems;
    IGameServices _services;
    public IGameServices Services
    {
        get
        {
            if (!_attached) throw new InvalidOperationException("Game context not attached.");
            return _services;
        }
    }

    IGameFlow _entryFlow;
    CancellationTokenSource _flowCts;

    protected override void Awake()
    {
        base.Awake();
    }

    void OnDestroy()
    {
        _flowCts?.Cancel();
        if (_subSystems != null)
        {
            foreach (var subSystem in _subSystems)
            {
                try
                {
                    subSystem.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to dispose SubSystem {subSystem.Name}: {e.Message}");
                }
            }
        }

        _services?.Clear();
        _flowCts?.Dispose();
    }

    public void AttachContext(IReadOnlyList<ISubSystem> subSystems, IGameServices services)
    {
        if (_attached) throw new InvalidOperationException("Context already attached.");
        if (subSystems == null) throw new ArgumentNullException(nameof(subSystems));
        if (services == null) throw new ArgumentNullException(nameof(services));

        _attached = true;
        _subSystems = new List<ISubSystem>(subSystems);
        _services = services;

        _flowCts = new CancellationTokenSource();
        _entryFlow = new DemoFlow(_services);
        _entryFlow.RunAsync(_flowCts.Token).Forget();
    }

}
