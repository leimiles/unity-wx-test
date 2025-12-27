using UnityEngine;
using YooAsset;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Threading;

public interface IYooService
{
    bool IsInitialized { get; }

    UniTask InitializeAsync(IProgress<float> progress);

    // 资源加载
    UniTask<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object;

    void ReleaseAsset<T>(string address);

    int GetActiveHandleCount();

    // 场景加载
    UniTask<SceneHandle> LoadSceneAsync(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single);

    // 批量预加载
    UniTask<int> PreloadAssetsAsync<T>(string[] addresses, IProgress<(int loaded, int total)> progress = null) where T : UnityEngine.Object;

    // 检查资源是否存在
    bool CheckAssetExists(string address);

    // 获取资源信息
    AssetInfo GetAssetInfo(string address);

    // 获取资源包信息
    PackageDetails GetPackageInfo();

    void ReleaseAllAssets();

    // 资源下载和管理
    ResourceDownloaderOperation CreateDownloader(int downloadingMaxNumber = 10, int failedTryAgain = 3);
    bool CheckNeedDownload(out int totalCount, out long totalBytes);
    UniTask DownloadResourcesAsync(IProgress<float> progress = null);
    UniTask UnloadUnusedAssetsAsync(IProgress<float> progress = null);

    void Dispose();
}

public sealed class YooService : IYooService
{
    private volatile bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    readonly YooSettings settings;

    ResourcePackage currentPackage;

    private readonly object _initGate = new();

    private UniTaskCompletionSource _initTcs;

    private readonly Dictionary<AssetKey, AssetHandleInfo> activeHandles = new();

    // 将 lock 改为 SemaphoreSlim
    private readonly SemaphoreSlim _handlesSemaphore = new SemaphoreSlim(1, 1);

    class AssetHandleInfo
    {
        public AssetHandle Handle { get; set; }
        public int RefCount { get; set; }
        public AssetHandleInfo(AssetHandle handle)
        {
            Handle = handle;
            RefCount = 1;
        }
    }

    public readonly struct AssetKey : IEquatable<AssetKey>
    {
        public readonly string Address;
        public readonly Type Type;

        public AssetKey(string address, Type type)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        public bool Equals(AssetKey other) =>
            string.Equals(Address, other.Address, StringComparison.Ordinal) &&
            Type == other.Type;

        public override bool Equals(object obj) =>
            obj is AssetKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Address, Type);
    }

    public YooService(YooSettings yooSettings)
    {
        settings = yooSettings != null ? yooSettings : throw new ArgumentNullException(nameof(yooSettings));
    }

    public UniTask InitializeAsync0(IProgress<float> progress)
    {
        UniTaskCompletionSource tcs;
        bool needStart = false;

        lock (_initGate)
        {
            // 在锁内检查初始化状态，避免竞态条件
            if (_isInitialized)
            {
                progress?.Report(1.0f);
                return UniTask.CompletedTask;
            }

            if (_initTcs == null)
            {
                _initTcs = new UniTaskCompletionSource();
                needStart = true;
            }
            tcs = _initTcs;
        }

        if (needStart)
        {
            RunInitialize(progress).Forget();
        }
        else
        {
            // 对于等待中的调用者，通知进度为 0（初始化已在进行中）
            progress?.Report(0f);
        }

        return tcs.Task;
    }
    public UniTask InitializeAsync(IProgress<float> progress)
    {
        UniTaskCompletionSource tcs;
        bool needStart = false;

        lock (_initGate)
        {
            // 在锁内检查初始化状态，避免竞态条件
            if (_isInitialized)
            {
                progress?.Report(1.0f);
                return UniTask.CompletedTask;
            }

            if (_initTcs == null)
            {
                _initTcs = new UniTaskCompletionSource();
                needStart = true;
            }
            tcs = _initTcs;
        }

        if (needStart)
        {
            RunInitialize(progress).Forget();
        }
        else
        {
            // 对于等待中的调用者，通知进度为 0（初始化已在进行中）
            progress?.Report(0f);
        }

        return tcs.Task;
    }

    private async UniTaskVoid RunInitialize(IProgress<float> progress)
    {
        try
        {
            await InitializeInternalAsync(progress);
            lock (_initGate)
            {
                _initTcs?.TrySetResult();
                _initTcs = null;
            }
        }
        catch (Exception ex)
        {
            lock (_initGate)
            {
                _initTcs?.TrySetException(ex);
                _initTcs = null; // 失败允许重试
            }
        }
    }

    async UniTask InitializeInternalAsync(IProgress<float> progress)
    {
        try
        {
            Debug.Log("=== Start YooAsset initialization ===");
            progress?.Report(0.0f);

            // 1. Initialize YooAsset
            if (!YooAssets.Initialized)
            {
                Debug.Log("Step 1: Initialize YooAsset...");
                YooAssets.Initialize();
            }
            progress?.Report(0.1f);

            // 2. Create package
            Debug.Log($"Step 2: Create package '{settings.packageName}'...");
            currentPackage = YooAssets.TryGetPackage(settings.packageName);
            currentPackage ??= YooAssets.CreatePackage(settings.packageName);
            progress?.Report(0.2f);

            // 3. Configure CDN address
            Debug.Log($"Step 3: Configure CDN address '{settings.hostServerURL}'...");
            string defaultHostServer = GetHostServerURL(settings.hostServerURL);
            string fallbackHostServer = string.IsNullOrEmpty(settings.hostServerFallbackURL) ? defaultHostServer : GetHostServerURL(settings.hostServerFallbackURL);
            IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            Debug.Log($"Main CDN address: {defaultHostServer}");
            Debug.Log($"Fallback CDN address: {fallbackHostServer}");
            progress?.Report(0.3f);

            // 3.5. Verify network connection
            if (settings.needNetworkVerified)
            {
                Debug.Log("Step 3.5: Verify network connection...");
                bool networkAvailable = await TestNetworkConnection(defaultHostServer);

                if (!networkAvailable && fallbackHostServer != defaultHostServer)
                {
                    Debug.LogWarning("Main CDN connection failed, try fallback CDN...");
                    networkAvailable = await TestNetworkConnection(fallbackHostServer);
                }

                progress?.Report(0.4f);

                if (!networkAvailable)
                {
                    string error = "Network connection verification failed! Unable to access CDN server";
                    Debug.LogError(error);
                    Debug.LogError($"Please check:");
                    Debug.LogError($"1. CDN address is correct: {settings.hostServerURL}");
                    Debug.LogError($"2. Network connection is normal");
                    Debug.LogError($"3. CDN server is accessible");
                    throw new InvalidOperationException(error);
                }
            }
            else
            {
                progress?.Report(0.4f);
            }

            // 4. Initialize resource package
            Debug.Log("Step 4: Initialize resource package...");
            InitializationOperation initOperation = null;

            switch (settings.playMode)
            {
                case EPlayMode.EditorSimulateMode:
                    var buildResult = EditorSimulateModeHelper.SimulateBuild(settings.packageName);
                    var packageRoot = buildResult.PackageRootDirectory;
                    var createParameters = new EditorSimulateModeParameters
                    {
                        EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot)
                    };
                    initOperation = currentPackage.InitializeAsync(createParameters);
                    break;
                case EPlayMode.OfflinePlayMode:
                    var createParametersOffline = new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                    };
                    initOperation = currentPackage.InitializeAsync(createParametersOffline);
                    break;
                case EPlayMode.HostPlayMode:
                    var createParametersHost = new HostPlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                        CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices)
                    };
                    initOperation = currentPackage.InitializeAsync(createParametersHost);
                    break;
                case EPlayMode.WebPlayMode:
                    var createParametersWeb = new WebPlayModeParameters
                    {
                        WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters()
                    };
                    initOperation = currentPackage.InitializeAsync(createParametersWeb);
                    break;
#if UNITY_WEBGL && WEIXINMINIGAME
                case EPlayMode.CustomPlayMode:
                    packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE";
                    var createParametersCustom = new WebPlayModeParameters
                    {
                        WebServerFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices)
                    };
                    initOperation = currentPackage.InitializeAsync(createParametersCustom);
                    break;
#endif
                default:
                    throw new System.Exception($"Unsupported play mode: {settings.playMode}");
            }

            await initOperation.ToUniTask();
            progress?.Report(0.5f);

            // Check initialization result
            if (initOperation.Status != EOperationStatus.Succeed)
            {
                throw new System.Exception($"Resource package initialization failed: {initOperation.Error}");
            }

            Debug.Log("Resource package initialization succeeded");

            // 5. Request package version
            string packageVersion = null;

            Debug.Log("Step 5: Request package version...");
            var versionOperation = currentPackage.RequestPackageVersionAsync(false);
            await versionOperation.ToUniTask();
            progress?.Report(0.65f);

            if (versionOperation.Status != EOperationStatus.Succeed)
            {
                throw new System.Exception($"Request package version failed: {versionOperation.Error}");
            }

            packageVersion = versionOperation.PackageVersion;
            Debug.Log($"Package version: <color=red>{packageVersion}</color>");

            // 6. Update package manifest
            Debug.Log("Step 6: Update package manifest...");
            var manifestOperation = currentPackage.UpdatePackageManifestAsync(packageVersion);
            await manifestOperation.ToUniTask();
            progress?.Report(0.85f);

            if (manifestOperation.Status != EOperationStatus.Succeed)
            {
                throw new System.Exception($"Update package manifest failed: {manifestOperation.Error}");
            }

            Debug.Log("Package manifest updated successfully");
            progress?.Report(0.85f);


            // 6.5 检查需要下载的资源
            Debug.Log("Step 6.5: Check resources need to download...");
            int downloadingMaxNumber = 10;
            int failedTryAgain = 3;
            var downloader = currentPackage.CreateResourceDownloader(downloadingMaxNumber, failedTryAgain);
            if (downloader == null)
            {
                throw new InvalidOperationException("[YooService] 创建下载器失败");
            }

            if (downloader.TotalDownloadCount > 0)
            {
                int totalDownloadCount = downloader.TotalDownloadCount;
                long totalDownloadBytes = downloader.TotalDownloadBytes;
                Debug.Log($"Found {totalDownloadCount} resources need to download, total size: {totalDownloadBytes / 1024.0 / 1024.0:F2} MB");

                // 自动更新：执行下载
                Debug.Log("Step 6.6: Auto-updating resources...");
                try
                {
                    // todo: 检查磁盘空间
                    // 开始下载
                    downloader.BeginDownload();

                    // 监控下载进度
                    while (!downloader.IsDone)
                    {
                        float downloadProgress = downloader.Progress;
                        // 将下载进度映射到 0.85-0.95 之间
                        float mappedProgress = 0.85f + downloadProgress * 0.1f;
                        progress?.Report(mappedProgress);
                        await UniTask.Yield();
                    }

                    // 检查下载结果
                    if (downloader.Status == EOperationStatus.Succeed)
                    {
                        Debug.Log($"Auto-update completed successfully: {totalDownloadCount} files downloaded");
                        progress?.Report(0.95f);
                    }
                    else
                    {
                        string error = downloader.Error ?? "未知错误";
                        Debug.LogError($"Auto-update failed: {error}");
                        throw new Exception($"资源自动更新失败: {error}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Auto-update exception: {ex.Message}");
                    throw; // 重新抛出异常，让上层处理
                }
            }
            else
            {
                Debug.Log("No resources need to download, skip update step");
                progress?.Report(0.9f);
            }

            // 6.7 Clear unused cache files
            Debug.Log("Step 6.7: Clear unused cache files...");
            var clearCacheOperation = currentPackage.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
            await clearCacheOperation.ToUniTask();

            if (clearCacheOperation.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"Clear cache completed successfully");
            }
            else
            {
                Debug.LogWarning($"Clear cache failed: {clearCacheOperation.Error}");
                // 清理缓存失败不影响初始化，只记录警告
            }
            progress?.Report(0.96f);

            // 7. Set default package
            YooAssets.SetDefaultPackage(currentPackage);
            Debug.Log("Default package set successfully");
            // 统一进度：如果有下载则从0.95继续，如果没有下载则从0.9继续
            progress?.Report(0.98f);

            _isInitialized = true;
            progress?.Report(1.0f);
            Debug.Log("=== YooAsset initialization completed ===");
            //OnInitialized?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"YooService initialization failed: {e}");
            //OnInitializeFailed?.Invoke(e.Message);
            throw;
        }
        finally
        {
            if (!_isInitialized)
            {
                // 只清理资源，_initTcs 的重置由 RunInitialize 的 catch 处理
                currentPackage = null;
            }
        }

    }

    /// <summary>
    /// 异步加载资源（UniTask 方式，带引用计数机制）
    /// </summary>
    public async UniTask<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
    {
        if (!_isInitialized || currentPackage == null)
            throw new InvalidOperationException($"[YooService] Not ready. IsInitialized={_isInitialized}, address={address}");

        AssetHandleInfo handleInfo = null;
        bool addedRef = false; // 标记是否增加了引用计数
        bool isNewHandle = false; // 标记是否是新创建的句柄

        AssetKey key = new AssetKey(address, typeof(T));

        // 使用 SemaphoreSlim 异步等待
        await _handlesSemaphore.WaitAsync();
        try
        {
            if (activeHandles.TryGetValue(key, out var existingHandleInfo))
            {
                handleInfo = existingHandleInfo;
                handleInfo.RefCount++;
                addedRef = true;
                Debug.Log($"[YooService] 资源已加载，引用计数: {handleInfo.RefCount}: {address}");
            }
            else
            {
                Debug.Log($"[YooService] 开始异步加载资源: {address}");
                var handle = currentPackage.LoadAssetAsync<T>(address);
                handleInfo = new AssetHandleInfo(handle);
                activeHandles[key] = handleInfo;
                isNewHandle = true;
            }
        }
        finally
        {
            _handlesSemaphore.Release();
        }

        // 等待加载完成（在锁外）
        await handleInfo.Handle.ToUniTask();

        // 检查加载结果
        if (handleInfo.Handle.Status == EOperationStatus.Succeed)
        {
            var asset = handleInfo.Handle.AssetObject as T ?? throw new InvalidCastException(
                    $"Asset '{address}' is not of type {typeof(T).Name}");
            return asset;

        }
        else
        {
            // 加载失败，需要回滚引用计数或清理句柄
            Debug.LogError($"[YooService] 加载失败: {address} - {handleInfo.Handle.LastError}");


            // 回滚操作也需要在锁内
            await _handlesSemaphore.WaitAsync();
            try
            {
                if (activeHandles.TryGetValue(key, out var info) && info == handleInfo)
                {
                    if (addedRef)
                    {
                        handleInfo.RefCount--;
                        Debug.Log($"[YooService] 加载失败，回滚引用计数: {handleInfo.RefCount}: {address}");
                    }
                    else if (isNewHandle)
                    {
                        activeHandles.Remove(key);
                        Debug.Log($"[YooService] 新句柄加载失败，已移除: {address}");
                    }
                }
            }
            finally
            {
                _handlesSemaphore.Release();
            }

            var error = handleInfo.Handle.LastError;

            // 如果是新创建的句柄，需要释放
            if (isNewHandle)
            {
                handleInfo.Handle.Release();
            }

            throw new Exception($"资源加载失败: {address} - {error}");
        }
    }

    /// <summary>
    /// 异步加载场景（UniTask 方式）
    /// 注意：激活场景要使用 await sceneHandle.ActivateAsync();
    /// 注意：释放场景要使用 await sceneHandle.UnloadAsync();
    /// </summary>
    public async UniTask<SceneHandle> LoadSceneAsync(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        if (!_isInitialized || currentPackage == null)
        {
            throw new InvalidOperationException($"[YooService] 未初始化，无法加载场景: {sceneName}");
        }

        Debug.Log($"[YooService] 开始加载场景: {sceneName}, LoadMode: {loadMode}");
        var handle = currentPackage.LoadSceneAsync(sceneName, loadMode);

        // 等待加载完成
        await handle.ToUniTask();

        // 检查加载结果
        if (handle.Status == EOperationStatus.Succeed)
        {
            Debug.Log($"[YooService] 场景加载成功: {sceneName}");
            return handle;
        }
        else
        {
            Debug.LogError($"[YooService] 场景加载失败: {sceneName} - {handle.LastError}");
            handle.Release();
            throw new Exception($"场景加载失败: {sceneName} - {handle.LastError}");
        }
    }

    /// <summary>
    /// 批量预加载资源（UniTask 方式）
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="addresses">资源地址数组</param>
    /// <param name="progress">进度回调，参数为 (已加载数量, 总数量)</param>
    /// 预加载时：调用 LoadAssetAsync，资源加载到内存，引用计数 = 1
    /// 使用时：再次调用 LoadAssetAsync，检测到已加载，直接返回资源对象，引用计数 +1
    /// 释放时：调用 ReleaseAsset，引用计数 -1；当引用计数 = 0 时，资源真正释放
    /// <returns>成功加载的资源数量</returns>
    public async UniTask<int> PreloadAssetsAsync<T>(
        string[] addresses,
        IProgress<(int loaded, int total)> progress = null)
        where T : UnityEngine.Object
    {
        if (!_isInitialized || currentPackage == null)
        {
            throw new InvalidOperationException("[YooService] 未初始化，无法预加载资源");
        }

        if (addresses == null || addresses.Length == 0)
        {
            Debug.LogWarning("[YooService] 预加载资源列表为空");
            return 0;
        }

        int total = addresses.Length;
        int loaded = 0;
        int successCount = 0;

        Debug.Log($"[YooService] 开始预加载 {total} 个资源");

        foreach (var address in addresses)
        {
            try
            {
                // 使用已有的 LoadAssetAsync 方法，它会自动处理引用计数
                await LoadAssetAsync<T>(address);
                successCount++;
                loaded++;
                progress?.Report((loaded, total));
                Debug.Log($"[YooService] 预加载成功 ({loaded}/{total}): {address}");
            }
            catch (Exception ex)
            {
                loaded++;
                progress?.Report((loaded, total));
                Debug.LogError($"[YooService] 预加载失败 ({loaded}/{total}): {address} - {ex.Message}");
                // 继续加载其他资源，不中断整个流程
            }
        }

        Debug.Log($"[YooService] 预加载完成: 成功 {successCount}/{total}");
        return successCount;
    }

    async UniTask<bool> TestNetworkConnection(string cdnBaseUrl)
    {
        string testUrl = $"{cdnBaseUrl}/{settings.networkVerifiedAssetName}";
        Debug.Log($"Test network connection: {testUrl}");

        using UnityWebRequest request = UnityWebRequest.Head(testUrl);
        // 设置超时时间（5秒）
        request.timeout = 5;
        //var response = await request.SendWebRequest();
        var operation = request.SendWebRequest();
        await operation.ToUniTask();

        bool success = false;
        switch (request.result)
        {
            case UnityWebRequest.Result.Success:
                Debug.Log($"Network connection successful: {testUrl} (status code: {request.responseCode})");
                success = true;
                break;
            case UnityWebRequest.Result.ConnectionError:
                Debug.LogError($"Network connection error: {testUrl} - {request.error}");
                success = false;
                break;
            case UnityWebRequest.Result.ProtocolError:
                if (request.responseCode == 404)
                {
                    Debug.LogWarning($"File not found (404): {testUrl}, but network connection is successful");
                    success = true;
                }
                else
                {
                    Debug.LogError($"Protocol error: {testUrl} - {request.responseCode} {request.error}");
                    success = false;
                }
                break;
            default:
                Debug.LogError($"Unknown error: {testUrl} - {request.error}");
                success = false;
                break;
        }
        return success;
    }

    public void ReleaseAsset<T>(string address)
    {
        var key = new AssetKey(address, typeof(T));

        if (!TryReleaseInternal(key, out var refCount))
        {
            Debug.LogWarning($"[YooService] ReleaseAsset failed (not loaded): {address} ({typeof(T).Name})");
            return;
        }

        if (refCount <= 0)
            Debug.Log($"[YooService] 已释放资源: {address}");
        else
            Debug.Log($"[YooService] 资源引用计数减少: {refCount}: {address}");
    }


    // 同步方法可以继续使用 lock，或者也改为 SemaphoreSlim
    private bool TryReleaseInternal(in AssetKey key, out int newRefCount)
    {
        _handlesSemaphore.Wait(); // 同步等待
        try
        {
            if (!activeHandles.TryGetValue(key, out var handleInfo))
            {
                newRefCount = 0;
                return false;
            }

            handleInfo.RefCount--;
            newRefCount = handleInfo.RefCount;

            if (handleInfo.RefCount <= 0)
            {
                handleInfo.Handle.Release();
                activeHandles.Remove(key);
            }

            return true;
        }
        finally
        {
            _handlesSemaphore.Release();
        }
    }

    /// <summary>
    /// 检查资源是否存在
    /// </summary>
    public bool CheckAssetExists(string address)
    {
        if (!_isInitialized || currentPackage == null)
        {
            Debug.LogWarning($"[YooService] 未初始化，无法检查资源: {address}");
            return false;
        }

        var assetInfo = currentPackage.GetAssetInfo(address);
        return assetInfo != null;
    }

    /// <summary>
    /// 获取资源信息
    /// </summary>
    public AssetInfo GetAssetInfo(string address)
    {
        if (!_isInitialized || currentPackage == null)
        {
            Debug.LogWarning($"[YooService] 未初始化，无法获取资源信息: {address}");
            return null;
        }

        return currentPackage.GetAssetInfo(address);
    }

    /// <summary>
    /// 获取资源包信息
    /// </summary>
    public PackageDetails GetPackageInfo()
    {
        if (!_isInitialized || currentPackage == null)
        {
            Debug.LogWarning("[YooService] 未初始化，无法获取包信息");
            return null;
        }

        return currentPackage.GetPackageDetails();
    }

    /// <summary>
    /// 释放所有资源句柄
    /// </summary>
    public void ReleaseAllAssets()
    {
        _handlesSemaphore.Wait();
        try
        {
            int count = activeHandles.Count;
            foreach (var kvp in activeHandles)
            {
                kvp.Value.Handle.Release();
            }
            activeHandles.Clear();
            Debug.Log($"[YooService] 已释放所有资源句柄 ({count} 个)");
        }
        finally
        {
            _handlesSemaphore.Release();
        }
    }

    /// <summary>
    /// 创建资源下载器（用于下载缺失的资源）
    /// </summary>
    public ResourceDownloaderOperation CreateDownloader(int downloadingMaxNumber = 10, int failedTryAgain = 3)
    {
        if (!_isInitialized || currentPackage == null)
        {
            Debug.LogError("[YooService] 未初始化，无法创建下载器");
            return null;
        }

        Debug.Log($"[YooService] 创建资源下载器，最大并发数: {downloadingMaxNumber}, 失败重试次数: {failedTryAgain}");
        return currentPackage.CreateResourceDownloader(downloadingMaxNumber, failedTryAgain);
    }

    /// <summary>
    /// 检查资源是否需要下载
    /// </summary>
    public bool CheckNeedDownload(out int totalCount, out long totalBytes)
    {
        totalCount = 0;
        totalBytes = 0;

        if (!_isInitialized || currentPackage == null)
        {
            return false;
        }

        var downloader = CreateDownloader();
        if (downloader == null)
        {
            return false;
        }

        totalCount = downloader.TotalDownloadCount;
        totalBytes = downloader.TotalDownloadBytes;
        return totalCount > 0;
    }

    /// <summary>
    /// 下载资源（UniTask 方式，带进度回调）
    /// </summary>
    public async UniTask DownloadResourcesAsync(IProgress<float> progress = null)
    {
        if (!_isInitialized || currentPackage == null)
        {
            throw new InvalidOperationException("[YooService] 未初始化，无法下载资源");
        }

        var downloader = CreateDownloader();
        if (downloader == null)
        {
            throw new InvalidOperationException("[YooService] 创建下载器失败");
        }

        if (downloader.TotalDownloadCount == 0)
        {
            Debug.Log("[YooService] 无需下载，所有资源已就绪");
            progress?.Report(1.0f);
            return;
        }

        Debug.Log($"[YooService] 开始下载资源: {downloader.TotalDownloadCount} 个文件，总大小: {downloader.TotalDownloadBytes / 1024 / 1024} MB");

        // 开始下载
        downloader.BeginDownload();

        // 监控进度
        while (!downloader.IsDone)
        {
            progress?.Report(downloader.Progress);
            await UniTask.Yield();
        }

        // 检查结果
        if (downloader.Status == EOperationStatus.Succeed)
        {
            Debug.Log("[YooService] 资源下载完成");
            progress?.Report(1.0f);
        }
        else
        {
            string error = downloader.Error ?? "未知错误";
            Debug.LogError($"[YooService] 资源下载失败: {error}");
            throw new Exception($"资源下载失败: {error}");
        }
    }

    /// <summary>
    /// 卸载未使用的资源（UniTask 方式，带进度回调）
    /// </summary>
    public async UniTask UnloadUnusedAssetsAsync(IProgress<float> progress = null)
    {
        if (!_isInitialized || currentPackage == null)
        {
            throw new InvalidOperationException("[YooService] 未初始化，无法卸载资源");
        }

        Debug.Log("[YooService] 开始卸载未使用的资源...");
        var operation = currentPackage.UnloadUnusedAssetsAsync();

        // 监控进度
        while (!operation.IsDone)
        {
            progress?.Report(operation.Progress);
            await UniTask.Yield();
        }

        Debug.Log("[YooService] 卸载未使用的资源完成");
        progress?.Report(1.0f);
    }


    public void Dispose()
    {
        // 先释放所有资源（需要在 semaphore 保护下进行）
        ReleaseAllAssets();

        // 释放 semaphore（在所有使用 semaphore 的操作完成后）
        _handlesSemaphore?.Dispose();

        currentPackage = null;
        _isInitialized = false;
        Debug.Log("[YooService] 已释放所有资源并重置服务");
    }


    /// <summary>
    /// 获取当前活跃的资源句柄数量
    /// </summary>
    public int GetActiveHandleCount()
    {
        _handlesSemaphore.Wait();
        try
        {
            return activeHandles.Count;
        }
        finally
        {
            _handlesSemaphore.Release();
        }
    }

    string GetHostServerURL(string baseUrl)
    {
        string platform = GetPlatformName();
        return $"{baseUrl}/{platform}/{settings.appVersion}";
    }

    string GetPlatformName()
    {
#if UNITY_EDITOR
        var buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
        return buildTarget switch
        {
            UnityEditor.BuildTarget.Android => "Android",
            UnityEditor.BuildTarget.iOS => "iOS",
            UnityEditor.BuildTarget.WebGL => "WebGL",
            _ => "PC",
        };
#else
        return Application.platform switch
        {
            RuntimePlatform.Android => "Android",
            RuntimePlatform.IPhonePlayer => "iOS",
            RuntimePlatform.WebGLPlayer => "WebGL",
            _ => "PC",
        };
#endif
    }

    class RemoteServices : IRemoteServices
    {
        readonly string _defaultHostServer;
        readonly string _fallbackHostServer;
        public RemoteServices(string defaultHostServer, string fallbackHostServer)
        {
            _defaultHostServer = defaultHostServer;
            _fallbackHostServer = fallbackHostServer;
        }

        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return $"{_defaultHostServer}/{fileName}";
        }

        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return $"{_fallbackHostServer}/{fileName}";
        }
    }

}
