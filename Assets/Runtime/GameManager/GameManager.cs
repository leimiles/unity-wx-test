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

    // 添加运行状态标志和锁
    private bool _isFlowRunning = false;
    private readonly object _flowLock = new object();

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
        if (_flowFactory == null)
            throw new InvalidOperationException("Flow factory not initialized.");

        var flow = _flowFactory.CreateFlow(flowID);

        // 使用 Fire-and-Forget 模式，但内部有保护
        RunGameFlowAsync(flow).Forget();
    }

    async UniTaskVoid RunGameFlowAsync(IGameFlow flow)
    {
        if (flow == null)
            throw new ArgumentNullException(nameof(flow));

        CancellationTokenSource previousCts = null;
        IGameFlow previousFlow = null;

        // 检查是否已有 Flow 在运行
        lock (_flowLock)
        {
            if (_isFlowRunning)
            {
                // 这是预期的行为（Flow 快速切换），使用 Debug 而不是 Warning
                Debug.Log($"[GameManager] Switching flow: cancelling previous flow and starting new one: {flow.GetType().Name}");

                // 保存之前的引用，以便在锁外取消
                previousCts = _flowCts;
                previousFlow = _currentFlow;
            }

            _flowCts = new CancellationTokenSource();
            _currentFlow = flow;
            _isFlowRunning = true;
        }

        // 在锁外取消之前的 Flow（避免在锁内执行可能耗时的操作）
        if (previousCts != null)
        {
            previousCts.Cancel();
            previousCts.Dispose();
        }

        try
        {
            await RunFlowInternalAsync(_currentFlow, _flowCts.Token);
        }
        catch (OperationCanceledException)
        {
            // 取消时，忽略（这是正常的 Flow 切换行为）
            Debug.Log($"[GameManager] Flow {flow.GetType().Name} was cancelled");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] Failed to run flow {flow.GetType().Name}: {e.Message}");
            // 可以考虑切换到错误 Flow
        }
        finally
        {
            lock (_flowLock)
            {
                // 只有在当前 Flow 完成时才清理（避免清理新创建的）
                if (_currentFlow == flow)
                {
                    _isFlowRunning = false;
                    _currentFlow = null;
                }
            }
        }
    }
    async UniTask RunFlowInternalAsync(IGameFlow flow, CancellationToken ct)
    {
        await flow.RunAsync(ct);
    }

    void OnDestroy()
    {
        lock (_flowLock)
        {
            _flowCts?.Cancel();
            _flowCts?.Dispose();
            _flowCts = null;
            _isFlowRunning = false;
        }

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
