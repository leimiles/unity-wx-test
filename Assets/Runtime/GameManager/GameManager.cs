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
    CancellationTokenSource _flowCts;
    IGameServices _services;
    IFlowFactory _flowFactory;
    IGameFlow _currentFlow;
    bool _flowSwitchHooked;
    EventBinding<RequestFlowSwitchEvent> _flowSwitchBinding;

    protected override void Awake()
    {
        base.Awake();
    }

    public void AttachContext(IReadOnlyList<ISubSystem> subSystems, IGameServices services)
    {
        if (_attached) throw new InvalidOperationException("Context already attached.");
        if (subSystems == null) throw new ArgumentNullException(nameof(subSystems));
        if (services == null) throw new ArgumentNullException(nameof(services));

        _attached = true;
        _subSystems = new List<ISubSystem>(subSystems);
        _services = services;

        _flowFactory = new FlowFactory(_services);

        HookFlowSwitch();

        RunFlow(FlowID.TestScene);
    }

    void HookFlowSwitch()
    {
        if (_flowSwitchHooked) return;
        _flowSwitchHooked = true;
        _flowSwitchBinding = new EventBinding<RequestFlowSwitchEvent>(OnRequestFlowSwitch);
        EventBus<RequestFlowSwitchEvent>.Register(_flowSwitchBinding);
    }

    void OnRequestFlowSwitch(RequestFlowSwitchEvent e)
    {
        Debug.Log($"OnRequestFlowSwitch: {e.Next}");
        if (!_attached || _flowFactory == null) return;
        RunFlow(e.Next);
    }


    public void RunFlow(FlowID flowID)
    {
        if (_flowFactory == null) throw new InvalidOperationException("Flow factory not initialized.");
        var flow = _flowFactory.CreateFlow(flowID);
        RunGameFlow(flow);
    }

    void RunGameFlow(IGameFlow flow)
    {
        if (flow == null)
        {
            throw new ArgumentNullException(nameof(flow));
        }

        _flowCts?.Cancel();
        _flowCts?.Dispose();

        _flowCts = new CancellationTokenSource();
        _currentFlow = flow;

        RunFlowInternalAsync(_currentFlow, _flowCts.Token).Forget();
    }

    async UniTaskVoid RunFlowInternalAsync(IGameFlow flow, CancellationToken ct)
    {
        try
        {
            await flow.RunAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // 取消时，忽略
            Debug.Log($"Flow {flow.GetType().Name} was cancelled");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to run flow {flow.GetType().Name}: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            // TODO: 考虑添加错误流程处理机制
            // 目前记录错误，防止应用崩溃
        }
    }

    void OnDestroy()
    {
        _flowCts?.Cancel();
        _flowCts?.Dispose();
        _flowCts = null;

        if (_flowSwitchHooked)
        {
            EventBus<RequestFlowSwitchEvent>.Deregister(_flowSwitchBinding);
            _flowSwitchBinding = null;
            _flowSwitchHooked = false;
        }

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

        // just in case
        _currentFlow = null;
        _flowFactory = null;

        _services = null;
        _subSystems = null;
        _attached = false;

    }

}
