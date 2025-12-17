using System.Collections.Generic;
using UnityEngine;
using MilesUtils;
using Cysharp.Threading.Tasks;
using System;

public class GameManager : PersistentSingleton<GameManager>
{
    EventBinding<BootstrapStartEvent> _bootstrapStartBinding;
    List<ISubSystem> _subSystems = new();
    public IGameServices Services { get; private set; }
    float _bootStartTime;

    protected override void Awake()
    {
        base.Awake();

        Services = new GameServices();

        _bootstrapStartBinding = new EventBinding<BootstrapStartEvent>(OnBootstrapStart);
        EventBus<BootstrapStartEvent>.Register(_bootstrapStartBinding);
    }

    void OnBootstrapStart(BootstrapStartEvent e)
    {
        if (e.bootstrapConfigs == null)
        {
            Debug.LogError("BootstrapConfigs is null");
            return;
        }

        StartBootSequence(e.bootstrapConfigs).Forget();
    }

    void OnDestroy()
    {
        if (_bootstrapStartBinding != null)
        {
            EventBus<BootstrapStartEvent>.Deregister(_bootstrapStartBinding);
            _bootstrapStartBinding = null;
        }
    }

    async UniTask StartBootSequence(BootstrapConfigs bootstrapConfigs)
    {
        _bootStartTime = Time.realtimeSinceStartup;
        Debug.Log($"Boot start at {_bootStartTime}");

        try
        {
            _subSystems.Clear();
            CreateSubSystems(bootstrapConfigs);

            float last = -1f;
            var progress = new Progress<float>(p =>
            {
                // 避免刷屏：只有变化到 1% 以上才打印
                if (p < last + 0.01f && p < 1f) return;
                last = p;

                EventBus<BootstrapProgressEvent>.Raise(
                    new BootstrapProgressEvent
                    {
                        progress = p,
                        message = "Bootstrap progress"
                    }
                );

                Debug.Log($"boot progress: {p * 100:F1}%");
            });

            await InitializeSubSystems(progress);

            float bootTotalTime = Time.realtimeSinceStartup - _bootStartTime;
            EventBus<BootstrapCompleteEvent>.Raise(
                new BootstrapCompleteEvent
                {
                    isSuccess = true,
                    message = "Bootstrap completed",
                    totalTime = bootTotalTime
                }
            );

            Debug.Log($"Bootstrap completed in {bootTotalTime:F1} seconds");
        }
        catch (Exception e)
        {
            Debug.LogError($"GameManager StartBootSequence Failed: {e.Message}");

            EventBus<BootstrapCompleteEvent>.Raise(
                new BootstrapCompleteEvent
                {
                    isSuccess = false,
                    message = e.Message,
                    totalTime = Time.realtimeSinceStartup - _bootStartTime
                }
            );
        }
    }

    async UniTask InitializeSubSystems(IProgress<float> progress)
    {
        int total = _subSystems.Count;
        if (total <= 0)
        {
            Debug.LogWarning("No subSystems to initialize");
            progress?.Report(1.0f);
            return;
        }

        int processed = 0; // 处理过的系统数（成功/失败都算）
        int succeeded = 0; // 成功数（仅用于日志/统计）

        Debug.Log($"InitializeSubSystems start, total {total} subSystems");

        // 只在初始化前排序一次（你原来就有）
        _subSystems.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        foreach (var subSystem in _subSystems)
        {
            EventBus<SubSystemInitializationStartEvent>.Raise(
                new SubSystemInitializationStartEvent
                {
                    subSystemName = subSystem.Name,
                    priority = subSystem.Priority
                });

            Debug.Log($"SubSystem {subSystem.Name} initialization started");

            string errorMessage = null;
            bool isSuccess = false;

            var subSystemProgress = new BootProgressMapper(progress, subSystem.Name, processed, total).Create();

            try
            {
                await subSystem.InitializeAsync(subSystemProgress);

                isSuccess = subSystem.IsInitialized;
                if (!isSuccess)
                {
                    errorMessage = $"SubSystem {subSystem.Name} initialization failed";
                    if (subSystem.IsRequired)
                    {
                        // Required：中断启动流程
                        throw new Exception(errorMessage);
                    }
                }
                else
                {
                    Debug.Log($"SubSystem {subSystem.Name} initialization completed");
                }
            }
            catch (Exception e)
            {
                isSuccess = false;
                errorMessage = e.Message;

                if (subSystem.IsRequired)
                {
                    Debug.LogError(
                        $"SubSystem {subSystem.Name} initialization failed: {e.Message}, but it is required, will throw exception");
                    throw; // Required：继续往上抛，让 StartBootSequence 统一失败
                }
                else
                {
                    Debug.LogError(
                        $"SubSystem {subSystem.Name} initialization failed: {e.Message}, but it is not required, will continue");
                    // Optional：吞掉异常继续
                }
            }
            finally
            {
                EventBus<SubSystemInitializationCompleteEvent>.Raise(
                    new SubSystemInitializationCompleteEvent
                    {
                        subSystemName = subSystem.Name,
                        isSuccess = isSuccess,
                        message = isSuccess ? "Initialization completed" : (errorMessage ?? "Initialization failed")
                    }
                );

                // 关键：先推进“处理计数”，保证总进度不会卡在 < 1
                processed++;

                if (isSuccess)
                    succeeded++;

                // 可选：每个系统完成时，给一个边界进度（避免最后一个系统没有上报 1.0）
                //progress?.Report(processed / (float)total);
            }
        }

        // 保底收口：无论 Optional 成功与否，只要流程跑完，进度都到 1.0
        progress?.Report(1.0f);
        Debug.Log($"InitializeSubSystems completed: {succeeded} / {total} subSystems initialized");
    }

    void CreateSubSystems(BootstrapConfigs bootstrapConfigs)
    {
        //1. 创建 YooUtilsSubSystem
        if (bootstrapConfigs.yooUtilsSettings != null)
        {
            var yooService = new YooService(bootstrapConfigs.yooUtilsSettings);
            Services.Register<IYooService>(yooService);

            var yooSubSystem = new YooSubSystem(Services.Get<IYooService>());
            RegisterSubSystem(yooSubSystem);
        }
        else
        {
            Debug.LogError("YooUtilsSettings is null, can't create YooUtilsSubSystem");
        }

        // 可以继续添加其他子系统
        // var testSubSystem = new TestSubSystem();
        // RegisterSubSystem(testSubSystem);

        // var failingTestSubSystem = new FailingTestSubSystem();
        // RegisterSubSystem(failingTestSubSystem);

        Debug.Log($"SubSystems created: {_subSystems.Count}");
    }

    void RegisterSubSystem(ISubSystem subSystem)
    {
        if (subSystem == null)
        {
            Debug.LogError("SubSystem is null, can't register");
            return;
        }

        var name = subSystem.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogError("SubSystem name is null/empty, can't register");
            return;
        }

        if (_subSystems.Exists(s => s.Name == name))
        {
            Debug.LogError($"SubSystem '{name}' already registered");
            return;
        }

        _subSystems.Add(subSystem);

        Debug.Log($"SubSystem '{name}' registered (Priority={subSystem.Priority}, Required={subSystem.IsRequired})");

    }

}
