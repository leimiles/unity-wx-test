using Cysharp.Threading.Tasks;
using System;
using YooAsset;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

public interface IGameSceneService
{
    bool IsInitialized { get; }
    UniTask InitializeAsync(IProgress<float> progress);
    // 场景加载（带资源管理）
    UniTask<SceneHandle> LoadSceneAsync(string sceneName, string[] assetAddresses = null, LoadSceneMode loadMode = LoadSceneMode.Single);
    UniTask UnloadSceneAsync(SceneHandle sceneHandle);
    // 场景切换（自动管理资源）
    UniTask SwitchSceneAsync(string newSceneName, string[] assetAddresses = null);
    void Dispose();
}

public sealed class GameSceneService : IGameSceneService, IDisposable
{
    private readonly IYooService _yooService;
    private volatile bool _isInitialized;

    // 记录场景使用的资源（用于自动释放）
    Dictionary<string, SceneRuntimeContext> _scenes;
    class SceneRuntimeContext
    {
        public SceneHandle Handle;
        public HashSet<string> Assets;
    }

    // 当前场景句柄（用于场景切换）
    private SceneHandle _currentSceneHandle;

    public bool IsInitialized => _isInitialized;

    public GameSceneService(IYooService yooService)
    {
        _yooService = yooService ?? throw new ArgumentNullException(nameof(yooService));
    }

    public UniTask InitializeAsync(IProgress<float> progress)
    {
        // 场景服务初始化（如果需要）
        _scenes = new Dictionary<string, SceneRuntimeContext>(StringComparer.Ordinal);

        _isInitialized = true;
        progress?.Report(1.0f);
        return UniTask.CompletedTask;
    }

    public async UniTask<SceneHandle> LoadSceneAsync(
        string sceneName,
        string[] assetAddresses = null,
        LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        if (!_yooService.IsInitialized)
        {
            throw new InvalidOperationException("[GameSceneService] YooService 未初始化");
        }

        // 1. 预加载场景需要的资源（如果有）
        if (assetAddresses != null && assetAddresses.Length > 0)
        {
            Debug.Log($"[GameSceneService] 预加载场景资源: {sceneName}, 资源数量: {assetAddresses.Length}");
            await _yooService.PreloadAssetsAsync<GameObject>(assetAddresses);
        }

        // 2. 加载场景
        var sceneHandle = await _yooService.LoadSceneAsync(sceneName, loadMode);

        // 3. 激活场景（使用同步方法）
        if (loadMode != LoadSceneMode.Single && !sceneHandle.ActivateScene())
        {
            Debug.LogWarning($"[GameSceneService] 场景激活失败: {sceneName}");
        }

        // 4. 记录场景使用的资源
        if (assetAddresses != null && assetAddresses.Length > 0)
        {
            _scenes[sceneName] = new SceneRuntimeContext
            {
                Handle = sceneHandle,
                Assets = new HashSet<string>(assetAddresses)
            };
        }

        // 5. 保存当前场景句柄
        _currentSceneHandle = sceneHandle;

        Debug.Log($"[GameSceneService] 场景加载完成: {sceneName}");
        return sceneHandle;
    }

    /// <summary>
    /// 卸载场景（自动释放资源）
    /// </summary>
    public async UniTask UnloadSceneAsync(SceneHandle sceneHandle)
    {
        if (sceneHandle == null)
        {
            Debug.LogWarning("[GameSceneService] SceneHandle 为 null，无法卸载");
            return;
        }

        // 1. 释放场景使用的资源
        if (_scenes.TryGetValue(sceneHandle.SceneName, out var context))
        {
            Debug.Log($"[GameSceneService] 释放场景资源，数量: {context.Assets.Count}");
            foreach (var address in context.Assets)
            {
                _yooService.ReleaseAsset<GameObject>(address);
            }
            _scenes.Remove(sceneHandle.SceneName);
        }

        // 2. 卸载场景
        var unloadOperation = sceneHandle.UnloadAsync();
        await unloadOperation.ToUniTask();

        // 3. 检查卸载结果
        if (unloadOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"[GameSceneService] 场景卸载失败: {unloadOperation.Error}");
            throw new Exception($"场景卸载失败: {unloadOperation.Error}");
        }

        // 4. 清理未使用的资源
        await _yooService.UnloadUnusedAssetsAsync();

        Debug.Log("[GameSceneService] 场景卸载完成");
    }

    /// <summary>
    /// 切换场景（自动卸载旧场景并加载新场景）
    /// </summary>
    public async UniTask SwitchSceneAsync(string newSceneName, string[] assetAddresses = null)
    {
        // 1. 如果有当前场景，先卸载
        if (_currentSceneHandle != null)
        {
            await UnloadSceneAsync(_currentSceneHandle);
            _currentSceneHandle = null;
        }
        else
        {
            // 即使没有句柄，也清理一下未使用的资源
            await _yooService.UnloadUnusedAssetsAsync();
        }

        // 2. 加载新场景（会自动设置 _currentSceneHandle）
        await LoadSceneAsync(newSceneName, assetAddresses, LoadSceneMode.Single);

        Debug.Log($"[GameSceneService] 场景切换完成: {newSceneName}");
    }

    public void Dispose()
    {
        _scenes.Clear();
        // yooService 已经由 YooService.Dispose 了，这里不需要再 Dispose
        _currentSceneHandle = null;
        _isInitialized = false;
    }
}