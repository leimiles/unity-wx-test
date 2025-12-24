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

        CancellationTokenSource newCts = new CancellationTokenSource();
        CancellationTokenSource previousCts = null;
        IGameFlow previousFlow = null;

        // 在锁内完成所有状态更新
        lock (_flowLock)
        {
            if (_isFlowRunning)
            {
                previousCts = _flowCts;
                previousFlow = _currentFlow;
                Debug.Log($"[GameManager] Flow switch: {previousFlow?.GetType().Name ?? "null"} -> {flow.GetType().Name}");
            }

            _flowCts = newCts;
            _currentFlow = flow;
            _isFlowRunning = true;
        }

        // 在锁外取消旧的 Flow，并等待取消传播
        if (previousCts != null)
        {
            try
            {
                previousCts.Cancel();
                // 等待一帧，确保取消操作传播到异步任务
                await UniTask.Yield();
            }
            finally
            {
                // 确保 Dispose，即使 Cancel 抛出异常
                previousCts.Dispose();
            }
        }

        try
        {
            // 使用传入的 flow 参数，而不是 _currentFlow（可能已被替换）
            await RunFlowInternalAsync(flow, newCts.Token);
        }
        catch (OperationCanceledException)
        {
            // 取消时，忽略（这是正常的 Flow 切换行为）
            Debug.Log($"[GameManager] Flow {flow.GetType().Name} was cancelled");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] Failed to run flow {flow.GetType().Name}: {e}");
            // 可以考虑切换到错误 Flow
        }
        finally
        {
            lock (_flowLock)
            {
                // 只有在当前 Flow 完成时才清理（避免清理新创建的）
                // 使用传入的 flow 参数进行比较，更安全
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

    protected override void OnDestroy()
    {
        // 先调用基类的清理逻辑（清理静态引用）
        base.OnDestroy();

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
