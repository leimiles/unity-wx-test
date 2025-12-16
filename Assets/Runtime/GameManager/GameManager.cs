using System.Collections.Generic;
using UnityEngine;
using MilesUtils;
using Cysharp.Threading.Tasks;
using System;

public class GameManager : PersistentSingleton<GameManager>
{
    EventBinding<BootstrapStartEvent> _bootstrapStartBinding;
    List<ISubSystem> _subSystems = new();
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

    async UniTask StartBootSequence(BootstrapConfigs bootstrapConfigs)
    {
        _bootStartTime = Time.realtimeSinceStartup;
        Debug.Log($"Boot start at {_bootStartTime}");

        try
        {
            _subSystems.Clear();
            CreateSubSystems(bootstrapConfigs);

            var progress = new Progress<float>(
                (p) =>
                {
                    Debug.Log($"Boot progress: {p * 100:F1}%");
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

    async UniTask InitializeSubSystems(IProgress<float> progress)
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

        // 只在初始化前排序一次
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

            try
            {
                var subSystemProgress = new Progress<float>(p =>
                {
                    p = Mathf.Clamp01(p);

                    float totalProgress = (completedSystems + p) / (float)totalSubSystems;
                    totalProgress = Mathf.Clamp01(totalProgress);

                    progress?.Report(totalProgress);

                    EventBus<SubSystemInitializationProgressEvent>.Raise(new SubSystemInitializationProgressEvent
                    {
                        subSystemName = subSystem.Name,
                        progress = p,
                        totalProgress = totalProgress
                    });
                });


                await subSystem.InitializeAsync(subSystemProgress);

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
                // 移除这里的重复进度报告，因为 subSystemProgress 回调已经报告过了
                // 只在最后确保进度是 100%（如果所有系统都完成）
                if (completedSystems == totalSubSystems)
                {
                    progress?.Report(1.0f);
                }
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

        if (_subSystems.Contains(subSystem))
        {
            Debug.LogError($"SubSystem {subSystem.Name} already registered, can't register again");
            return;
        }
        // 只添加，不排序
        _subSystems.Add(subSystem);

        Debug.Log($"SubSystem {subSystem.Name} registered successfully");
    }

}
