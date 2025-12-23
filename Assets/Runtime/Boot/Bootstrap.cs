using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class Bootstrap : MonoBehaviour
{
    [SerializeField] int frameRate = 60;
    [SerializeField] bool runInBackground = true;
    [SerializeField] BootstrapConfigs bootstrapConfigs;
    float _bootStartTime;
    EventBinding<BootstrapCompleteEvent> _bootCompleteBinding;
    readonly List<ISubSystem> _subSystems = new();
    IGameServices _services;
    GameObject _bootUI;
    const string kBootUIPath = "UI/Canvas_Boot";

    void Awake()
    {
        Application.targetFrameRate = frameRate;
        Application.runInBackground = runInBackground;

        _bootCompleteBinding = new EventBinding<BootstrapCompleteEvent>(OnBootComplete);
        EventBus<BootstrapCompleteEvent>.Register(_bootCompleteBinding);

    }

    void Start()
    {
        try
        {
            ShowBootUI();
            if (bootstrapConfigs == null)
            {
                Debug.LogError("BootstrapConfigs is null, cannot start bootstrap");
                EventBus<BootstrapCompleteEvent>.Raise(
                    new BootstrapCompleteEvent
                    {
                        isSuccess = false,
                        message = "BootstrapConfigs is null",
                        totalTime = 0f
                    }
                );
                return;
            }
            bootstrapConfigs.Validate();
            _services = new GameServices();
            var gameManager = GameManager.Instance; // 确保 GameManager 已经初始化
            StartBootSequence(bootstrapConfigs).Forget();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Bootstrap Start failed: {e}");
            EventBus<BootstrapCompleteEvent>.Raise(
                new BootstrapCompleteEvent
                {
                    isSuccess = false,
                    message = e.Message,
                    totalTime = 0f
                }
            );
        }
    }

    void OnBootComplete(BootstrapCompleteEvent e)
    {
        if (e.isSuccess)
        {
            Debug.Log("Bootstrap complete");
            //将子系统列表传递给GameManager，系统由GameManager管理
            GameManager.Instance.AttachContext(_subSystems, _services);
            //自毁
            Destroy(gameObject);
        }
        else
        {
            Debug.LogError("Bootstrap failed: " + e.message);
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
            Debug.LogError($"Bootstrap StartBootSequence Failed: {e.Message}");

            // 失败兜底：释放已创建的子系统资源
            foreach (var s in _subSystems)
            {
                try { s.Dispose(); } catch { }
            }
            _subSystems.Clear();
            _services?.Clear();
            _services = null;

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

    void CreateSubSystems(BootstrapConfigs bootstrapConfigs)
    {
        // 创建 YooSubSystem
        var yooService = new YooService(bootstrapConfigs.yooSettings);
        _services.Register<IYooService>(yooService);
        var yooSubSystem = new YooSubSystem(yooService);
        RegisterSubSystem(yooSubSystem);

        // 创建 GameSceneSubSystem
        var gameSceneService = new GameSceneService(yooService);
        var gameSceneSubSystem = new GameSceneSubSystem(gameSceneService);
        _services.Register<IGameSceneService>(gameSceneService);
        RegisterSubSystem(gameSceneSubSystem);

        // 创建 GameWorldSubSystem
        var gameWorldService = new GameWorldService();
        var gameWorldSubSystem = new GameWorldSubSystem(gameWorldService);
        _services.Register<IGameWorldService>(gameWorldService);
        RegisterSubSystem(gameWorldSubSystem);

        // ------------------------------------------------------------
        // 可以继续添加其他子系统
        // var testSubSystem = new TestSubSystem();
        // RegisterSubSystem(testSubSystem);

        // var failingTestSubSystem = new FailingTestSubSystem();
        // RegisterSubSystem(failingTestSubSystem);

        Debug.Log($"SubSystems created: {_subSystems.Count}");
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

    void OnDestroy()
    {
        if (_bootCompleteBinding != null)
        {
            EventBus<BootstrapCompleteEvent>.Deregister(_bootCompleteBinding);
            _bootCompleteBinding = null;
        }
    }

    /// <summary>
    /// 显示 BootUI，在 UISubSystem 初始化完成后切换到 CrossUI
    /// </summary>
    void ShowBootUI()
    {
        var bootUI = Resources.Load<GameObject>(kBootUIPath);
        if (bootUI == null)
        {
            Debug.LogError($"BootUI prefab not found: Resources/{kBootUIPath}.prefab");
            return;
        }
        _bootUI = Instantiate(bootUI);
        _bootUI.name = "[BootstrapUI] Boot";
    }
}
