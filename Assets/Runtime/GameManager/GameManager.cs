using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MilesUtils;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
public class GameManager : PersistentSingleton<GameManager>
{
    EventBinding<BootstrapStartEvent> _bootstrapStartBinding;
    Dictionary<string, ISubSystem> _subSystems = new();
    float _bootStartTime;

    protected override void Awake()
    {
        base.Awake();

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

    async UniTaskVoid StartBootSequence(BootstrapConfigs bootstrapConfigs)
    {
        _bootStartTime = Time.realtimeSinceStartup;
        Debug.Log($"Boot start at {_bootStartTime}");

        try
        {
            CreateSubSystems(bootstrapConfigs);

            var progress = new Progress<float>(
                (p) =>
                {
                    Debug.Log($"Boot progress: {p * 100:F1}%");
                    if (p > 1.0)
                    {
                        Debug.LogError("Boot progress reported greater than 100%");
                    }
                }
            );

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

    async UniTask InitializeSubSystems(IProgress<float> progress, CancellationToken ct = default)
    {
        int totalSubSystems = _subSystems.Count;
        if (totalSubSystems <= 0)
        {
            Debug.LogWarning("No subSystems to initialize");
            progress?.Report(1.0f);
            return;
        }

        int completedSystems = 0;
        Debug.Log($"InitializeSubSystems start, total {totalSubSystems} subSystems");

        // 先按照优先级排序
        var sortedSubSystems = new List<ISubSystem>(_subSystems.Values);
        sortedSubSystems.Sort((a, b) => a.Priority.CompareTo(b.Priority));

        foreach (var subSystem in sortedSubSystems)
        {
            ct.ThrowIfCancellationRequested();

            EventBus<SubSystemInitializationStartEvent>.Raise(
                new SubSystemInitializationStartEvent
                {
                    subSystemName = subSystem.Name,
                    priority = subSystem.Priority
                });

            Debug.Log($"SubSystem {subSystem.Name} initialization started");

            string errorMessage = null;
            bool isSuccess = false;

            try
            {
                // 创建进度报告器，将子系统进度映射到总进度
                var subSystemProgress = new Progress<float>(
                    (p) =>
                    {
                        float totalProgress = (completedSystems + p) / totalSubSystems;
                        progress?.Report(totalProgress);

                        EventBus<SubSystemInitializationProgressEvent>.Raise(
                            new SubSystemInitializationProgressEvent
                            {
                                progress = p,
                                totalProgress = totalProgress
                            }
                        );
                    }
                );

                await subSystem.InitializeAsync(subSystemProgress, ct);

                isSuccess = subSystem.IsInitialized;

                if (!isSuccess)
                {
                    errorMessage = $"SubSystem {subSystem.Name} initialization failed";
                    if (subSystem.IsRequired)
                    {
                        throw new Exception($"SubSystem {subSystem.Name} initialization failed, but it is required");
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
                    Debug.LogError($"SubSystem {subSystem.Name} initialization failed: {e.Message}, but it is required, will throw exception");
                    throw;
                }
                else
                {
                    Debug.LogError($"SubSystem {subSystem.Name} initialization failed: {e.Message}, but it is not required, will log error and continue");
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

                if (isSuccess)
                {
                    completedSystems++;
                }
                progress?.Report((float)completedSystems / totalSubSystems);
            }
        }
        Debug.Log($"InitializeSubSystems completed: {completedSystems} / {totalSubSystems} subSystems initialized");
    }

    void CreateSubSystems(BootstrapConfigs bootstrapConfigs)
    {
        //1. 创建 YooUtilsSubSystem
        if (bootstrapConfigs.yooUtilsSettings != null)
        {
            var yooUtils = new YooUtilsByUniTask(bootstrapConfigs.yooUtilsSettings);
            RegisterSubSystem(yooUtils);
        }
        else
        {
            Debug.LogError("YooUtilsSettings is null, can't create YooUtilsSubSystem");
        }

        // 可以继续添加其他子系统

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

        if (!_subSystems.TryAdd(name, subSystem))
        {
            Debug.LogError($"SubSystem {name} already registered, can't register again");
            return;
        }

        Debug.Log($"SubSystem {name} registered successfully");
    }

}
