using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using UnityEngine.Networking;
using MilesUtils;

/// <summary>
/// YooAsset 工具类 - 优化版
/// </summary>
[DisallowMultipleComponent]
public class YooUtils : PersistentSingleton<YooUtils>
{
    [SerializeField] private YooSettings settings;
    [SerializeField] private EPlayMode playMode;

    // 初始化状态
    private bool isInitialized = false;
    private bool isInitializing = false;
    private ResourcePackage currentPackage;

    // 事件回调
    public event Action OnInitialized;
    public event Action<string> OnInitializeFailed;

    // 资源句柄管理（带引用计数）
    private Dictionary<string, AssetHandleInfo> activeHandles = new();

    private class AssetHandleInfo
    {
        public AssetHandle Handle { get; set; }
        public int RefCount { get; set; }

        public AssetHandleInfo(AssetHandle handle)
        {
            Handle = handle;
            RefCount = 1;
        }
    }

    protected override void Awake()
    {
        base.Awake(); // 调用 PersistentSingleton 的 Awake，自动处理 DontDestroyOnLoad
    }

    void Start()
    {
        if (!isInitialized && !isInitializing)
        {
            StartCoroutine(Initialize());
        }
    }

    /// <summary>
    /// 初始化 YooAsset
    /// </summary>
    public IEnumerator Initialize()
    {
        if (isInitialized)
        {
            Debug.Log("YooAsset 已经初始化，跳过");
            yield break;
        }

        if (isInitializing)
        {
            Debug.LogWarning("YooAsset 正在初始化中，请等待...");
            while (isInitializing)
                yield return null;
            yield break;
        }

        isInitializing = true;

        // 验证配置
        if (settings == null)
        {
            string error = "YooSettings 配置为空！请在 Inspector 中分配设置资源";
            Debug.LogError(error);
            OnInitializeFailed?.Invoke(error);
            isInitializing = false;
            yield break;
        }

        Debug.Log("=== 开始 YooAsset 初始化 ===");

        // 初始化流程
        {
            // 1. 初始化 YooAsset（检查是否已初始化）
            if (!YooAssets.Initialized)
            {
                Debug.Log("步骤 1: 初始化 YooAsset...");
                YooAssets.Initialize();
            }
            else
            {
                Debug.Log("步骤 1: YooAsset 已初始化，跳过");
            }

            // 2. 创建资源包
            Debug.Log($"步骤 2: 创建资源包 '{settings.packageName}'...");
            currentPackage = YooAssets.TryGetPackage(settings.packageName);
            if (currentPackage == null)
            {
                currentPackage = YooAssets.CreatePackage(settings.packageName);
            }

            // 3. 配置 CDN 地址
            Debug.Log($"步骤 3: 配置 CDN 地址 '{settings.hostServerURL}'...");
            string defaultHostServer = GetHostServerURL(settings.hostServerURL);
            string fallbackHostServer = string.IsNullOrEmpty(settings.hostServerFallbackURL)
                ? defaultHostServer
                : GetHostServerURL(settings.hostServerFallbackURL);

            IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            Debug.Log($"主 CDN 地址: {defaultHostServer}");
            Debug.Log($"备用 CDN 地址: {fallbackHostServer}");

            // 3.5. 验证网络连接
            if (settings.needNetworkVerified)
            {
                Debug.Log("步骤 3.5: 验证网络连接...");
                bool networkAvailable = false;
                yield return TestNetworkConnection(defaultHostServer, (result) => networkAvailable = result);

                if (!networkAvailable && fallbackHostServer != defaultHostServer)
                {
                    Debug.LogWarning("主 CDN 连接失败，尝试备用 CDN...");
                    yield return TestNetworkConnection(fallbackHostServer, (result) => networkAvailable = result);
                }

                if (!networkAvailable)
                {
                    string error = "网络连接验证失败！无法访问 CDN 服务器";
                    Debug.LogError(error);
                    Debug.LogError($"请检查：");
                    Debug.LogError($"1. CDN 地址是否正确: {settings.hostServerURL}");
                    Debug.LogError($"2. 网络连接是否正常");
                    Debug.LogError($"3. CDN 服务器是否可访问");
                    OnInitializeFailed?.Invoke(error);
                    isInitializing = false;
                    yield break;
                }
            }

            // 4. 初始化资源包
            Debug.Log("步骤 4: 初始化资源包...");
            InitializationOperation initOperation = null;

            if (playMode == EPlayMode.HostPlayMode)
            {
                var createParameters = new HostPlayModeParameters
                {
                    BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                    CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices)
                };
                initOperation = currentPackage.InitializeAsync(createParameters);
            }
            else if (playMode == EPlayMode.WebPlayMode)
            {
                var createParameters = new WebPlayModeParameters
                {
                    WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters()
                };
                initOperation = currentPackage.InitializeAsync(createParameters);
            }
#if UNITY_WEBGL && WEIXINMINIGAME
            else if (playMode == EPlayMode.CustomPlayMode)
            {
                // Use CustomPlayMode as WX MiniGame
                string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/_GAME_FILE_CACHE/yoo";
                var createParameters = new WebPlayModeParameters
                {
                    WebServerFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices, null)
                };
                initOperation = currentPackage.InitializeAsync(createParameters);
            }
#endif
            else
            {
                string error = $"不支持的运行模式: {playMode}，请使用 HostPlayMode、WebPlayMode 或 CustomPlayMode";
                Debug.LogError(error);
                OnInitializeFailed?.Invoke(error);
                isInitializing = false;
                yield break;
            }

            yield return initOperation;

            // 5. 检查初始化结果
            if (initOperation.Status != EOperationStatus.Succeed)
            {
                string error = $"资源包初始化失败: {initOperation.Error}";
                Debug.LogError(error);
                OnInitializeFailed?.Invoke(error);
                isInitializing = false;
                yield break;
            }

            Debug.Log("✓ 资源包初始化成功！");

            // 6. 请求资源版本（HostPlayMode 和 WebPlayMode 需要）
            string packageVersion = null;
            if (playMode == EPlayMode.HostPlayMode || playMode == EPlayMode.WebPlayMode || playMode == EPlayMode.CustomPlayMode)
            {
                Debug.Log("步骤 5: 请求资源版本...");
                var versionOperation = currentPackage.RequestPackageVersionAsync(false);
                yield return versionOperation;

                if (versionOperation.Status != EOperationStatus.Succeed)
                {
                    string error = $"请求资源版本失败: {versionOperation.Error}";
                    Debug.LogError(error);
                    Debug.LogError("请确保 CDN 服务器上存在 PackageVersion.txt 文件");
                    OnInitializeFailed?.Invoke(error);
                    isInitializing = false;
                    yield break;
                }

                packageVersion = versionOperation.PackageVersion;
                Debug.Log($"✓ 获取到资源版本: {packageVersion}");
            }

            // 7. 更新资源清单（HostPlayMode 和 WebPlayMode 需要）
            if (playMode == EPlayMode.HostPlayMode || playMode == EPlayMode.WebPlayMode || playMode == EPlayMode.CustomPlayMode)
            {
                Debug.Log("步骤 6: 更新资源清单...");
                var manifestOperation = currentPackage.UpdatePackageManifestAsync(packageVersion);
                yield return manifestOperation;

                if (manifestOperation.Status != EOperationStatus.Succeed)
                {
                    string error = $"更新资源清单失败: {manifestOperation.Error}";
                    Debug.LogError(error);
                    Debug.LogError("请确保 CDN 服务器上存在对应的 manifest 文件");
                    OnInitializeFailed?.Invoke(error);
                    isInitializing = false;
                    yield break;
                }

                Debug.Log("✓ 资源清单更新成功！");
            }

            // 8. 设置默认资源包
            YooAssets.SetDefaultPackage(currentPackage);
            Debug.Log("✓ 已设置默认资源包");

            isInitialized = true;
            isInitializing = false;
            Debug.Log("=== YooAsset 初始化完成 ===");
            OnInitialized?.Invoke();
        }
    }

    #region 资源加载 API

    /// <summary>
    /// 异步加载资源（协程方式）
    /// </summary>
    public IEnumerator LoadAssetRoutine<T>(
        string address,
        System.Action<T> onSuccess = null,
        System.Action<string> onFail = null)
        where T : UnityEngine.Object
    {
        if (!WaitForInitialization())
        {
            string error = $"未初始化，无法加载资源: {address}";
            Debug.LogError($"[YooUtils] {error}");
            onFail?.Invoke(error);
            yield break;
        }

        Debug.Log($"[YooUtils] 开始加载资源: {address}");

        // 先检查是否已存在（避免重复加载）
        if (activeHandles.TryGetValue(address, out var existingHandleInfo))
        {
            // 如果已存在，等待已存在的句柄完成（如果还没完成）
            yield return existingHandleInfo.Handle;

            if (existingHandleInfo.Handle.Status == EOperationStatus.Succeed)
            {
                existingHandleInfo.RefCount++;
                Debug.Log($"[YooUtils] 资源已加载，引用计数: {existingHandleInfo.RefCount}: {address}");
                onSuccess?.Invoke(existingHandleInfo.Handle.AssetObject as T);
            }
            else
            {
                // 已存在的句柄失败，清理它以便后续重新尝试加载
                Debug.LogError($"[YooUtils] 资源加载失败: {address}");
                Debug.LogError(existingHandleInfo.Handle.LastError);
                ReleaseAsset(address);  // 失败时释放句柄
                onFail?.Invoke(existingHandleInfo.Handle.LastError);
            }
            yield break;
        }

        // 不存在，创建新的句柄
        var handle = YooAssets.LoadAssetAsync<T>(address);
        yield return handle;

        if (handle.Status == EOperationStatus.Succeed)
        {
            T asset = handle.AssetObject as T;
            // 记录句柄用于后续释放（使用引用计数机制）
            activeHandles[address] = new AssetHandleInfo(handle);
            onSuccess?.Invoke(asset);
        }
        else
        {
            Debug.LogError($"[YooUtils] 加载失败: {address}");
            Debug.LogError(handle.LastError);
            onFail?.Invoke(handle.LastError);
            handle.Release();
        }
    }

    /// <summary>
    /// 异步加载资源（返回句柄，需要手动释放）
    /// 带引用计数机制
    /// 注意：
    /// - 句柄会立即添加到字典中，即使加载还未完成
    /// - 调用者需要等待句柄完成（yield return handle 或 await handle.ToUniTask()）
    /// - 如果加载失败，调用者应该调用 ReleaseAsset(address) 来清理句柄
    /// - 如果加载成功，句柄会保留在字典中，供后续复用
    /// </summary>
    public AssetHandle LoadAssetAsync<T>(string address) where T : UnityEngine.Object
    {
        if (!WaitForInitialization())
        {
            Debug.LogError($"[YooUtils] 未初始化，无法加载资源: {address}");
            return null;
        }

        if (activeHandles.TryGetValue(address, out var handleInfo))
        {
            handleInfo.RefCount++;
            Debug.Log($"[YooUtils] 资源已加载，引用计数: {handleInfo.RefCount}: {address}");
            return handleInfo.Handle;
        }

        Debug.Log($"[YooUtils] 开始异步加载资源: {address}");
        var handle = YooAssets.LoadAssetAsync<T>(address);
        activeHandles[address] = new AssetHandleInfo(handle);
        return handle;
    }

    /// <summary>
    /// 同步加载资源（会阻塞，不推荐在主线程使用），暂不使用
    /// </summary>
    // public T LoadAssetSync<T>(string address) where T : UnityEngine.Object
    // {
    //     if (!WaitForInitialization())
    //     {
    //         LogError($"[YooUtils] 未初始化，无法加载资源: {address}");
    //         return null;
    //     }

    //     Log(2, $"[YooUtils] 同步加载资源: {address}（注意：会阻塞主线程）");
    //     var handle = YooAssets.LoadAssetSync<T>(address);

    //     if (handle != null)
    //     {
    //         if (!activeHandles.ContainsKey(address))
    //         {
    //             activeHandles[address] = handle;
    //         }
    //         return handle.AssetObject as T;
    //     }

    //     return null;
    // }

    /// <summary>
    /// 加载场景
    /// </summary>
    public SceneHandle LoadSceneAsync(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        if (!WaitForInitialization())
        {
            Debug.LogError($"[YooUtils] 未初始化，无法加载场景: {sceneName}");
            return null;
        }

        Debug.Log($"[YooUtils] 开始加载场景: {sceneName}");
        var handle = YooAssets.LoadSceneAsync(sceneName, loadMode);
        return handle;
    }

    /// <summary>
    /// 创建资源下载器（用于下载缺失的资源）
    /// </summary>
    public ResourceDownloaderOperation CreateDownloader(int downloadingMaxNumber = 10, int failedTryAgain = 3)
    {
        if (!WaitForInitialization())
        {
            Debug.LogError("[YooUtils] 未初始化，无法创建下载器");
            return null;
        }

        Debug.Log($"[YooUtils] 创建资源下载器，最大并发数: {downloadingMaxNumber}, 失败重试次数: {failedTryAgain}");
        return currentPackage.CreateResourceDownloader(downloadingMaxNumber, failedTryAgain);
    }

    /// <summary>
    /// 检查资源是否需要下载
    /// </summary>
    public bool CheckNeedDownload(out int totalCount, out long totalBytes)
    {
        totalCount = 0;
        totalBytes = 0;

        if (!WaitForInitialization())
            return false;

        var downloader = CreateDownloader();
        if (downloader == null)
            return false;

        totalCount = downloader.TotalDownloadCount;
        totalBytes = downloader.TotalDownloadBytes;
        return totalCount > 0;
    }

    /// <summary>
    /// 下载资源（协程方式，带进度回调）
    /// </summary>
    public IEnumerator DownloadResources(
        System.Action<float> onProgress = null,
        System.Action onComplete = null,
        System.Action<string> onError = null)
    {
        if (!WaitForInitialization())
        {
            onError?.Invoke("未初始化");
            yield break;
        }

        var downloader = CreateDownloader();
        if (downloader == null)
        {
            onError?.Invoke("创建下载器失败");
            yield break;
        }

        if (downloader.TotalDownloadCount == 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        // 开始下载
        downloader.BeginDownload();

        // 监控进度
        while (!downloader.IsDone)
        {
            onProgress?.Invoke(downloader.Progress);
            yield return null;
        }

        // 检查结果
        if (downloader.Status == EOperationStatus.Succeed)
        {
            onComplete?.Invoke();
        }
        else
        {
            onError?.Invoke(downloader.Error);
        }
    }

    /// <summary>
    /// 预加载资源列表
    /// </summary>
    public IEnumerator PreloadAssets<T>(
        string[] addresses,
        System.Action<int, int> onProgress = null,
        System.Action onComplete = null,
        System.Action<string> onError = null) where T : UnityEngine.Object
    {
        if (!WaitForInitialization())
        {
            onError?.Invoke("未初始化");
            yield break;
        }

        int total = addresses.Length;
        int loaded = 0;

        foreach (var address in addresses)
        {
            var handle = LoadAssetAsync<T>(address);
            if (handle == null)
            {
                onError?.Invoke($"预加载失败: {address} - 无法创建加载句柄");
                continue;
            }

            yield return handle;

            if (handle.Status == EOperationStatus.Succeed)
            {
                loaded++;
                onProgress?.Invoke(loaded, total);
            }
            else
            {
                Debug.LogWarning($"[YooUtils] 预加载失败，释放句柄: {address}");
                ReleaseAsset(address);  // 失败时释放句柄
                onError?.Invoke($"预加载失败: {address} - {handle.LastError}");
            }
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// 检查资源是否存在
    /// </summary>
    public bool CheckAssetExists(string address)
    {
        if (!WaitForInitialization())
            return false;

        var location = currentPackage.GetAssetInfo(address);
        return location != null;
    }

    /// <summary>
    /// 获取资源信息
    /// </summary>
    public AssetInfo GetAssetInfo(string address)
    {
        if (!WaitForInitialization())
            return null;

        return currentPackage.GetAssetInfo(address);
    }

    /// <summary>
    /// 获取资源包信息
    /// </summary>
    public PackageDetails GetPackageInfo()
    {
        if (!WaitForInitialization())
            return null;

        return currentPackage.GetPackageDetails();
    }

    /// <summary>
    /// 获取当前活跃的资源句柄数量
    /// </summary>
    public int GetActiveHandleCount()
    {
        return activeHandles.Count;
    }

    /// <summary>
    /// 释放资源句柄
    /// 带引用计数机制
    /// </summary>
    public void ReleaseAsset(string address)
    {
        if (activeHandles.TryGetValue(address, out var handleInfo))
        {
            handleInfo.RefCount--;
            if (handleInfo.RefCount <= 0)
            {
                handleInfo.Handle.Release();
                activeHandles.Remove(address);
                Debug.Log($"[YooUtils] 已释放资源: {address}");
            }
            else
            {
                Debug.Log($"[YooUtils] 资源引用计数减少: {handleInfo.RefCount}: {address}");
            }
        }
        else
        {
            Debug.LogWarning($"[YooUtils] 未找到资源句柄: {address}");
        }
    }

    /// <summary>
    /// 释放所有资源句柄
    /// </summary>
    public void ReleaseAllAssets()
    {
        foreach (var kvp in activeHandles)
        {
            kvp.Value.Handle.Release();
        }
        activeHandles.Clear();
        Debug.Log("[YooUtils] 已释放所有资源句柄");
    }

    /// <summary>
    /// 卸载未使用的资源（带回调）
    /// </summary>
    public IEnumerator UnloadUnusedAssets(
        System.Action<float> onProgress = null,
        System.Action onComplete = null)
    {
        if (currentPackage == null)
        {
            Debug.LogError("[YooUtils] 资源包未初始化");
            yield break;
        }

        Debug.Log("[YooUtils] 开始卸载未使用的资源...");
        var operation = currentPackage.UnloadUnusedAssetsAsync();

        while (!operation.IsDone)
        {
            onProgress?.Invoke(operation.Progress);
            yield return null;
        }

        Debug.Log("[YooUtils] 卸载未使用的资源完成");
        onComplete?.Invoke();
    }


    /// <summary>
    /// 检查资源版本更新
    /// </summary>
    public IEnumerator CheckVersionUpdate(
        System.Action<string> onVersionChecked = null,
        System.Action<string> onError = null)
    {
        if (!WaitForInitialization())
        {
            onError?.Invoke("未初始化");
            yield break;
        }

        var versionOperation = currentPackage.RequestPackageVersionAsync(false);
        yield return versionOperation;

        if (versionOperation.Status == EOperationStatus.Succeed)
        {
            onVersionChecked?.Invoke(versionOperation.PackageVersion);
        }
        else
        {
            onError?.Invoke(versionOperation.Error);
        }
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 等待初始化完成
    /// </summary>
    private bool WaitForInitialization()
    {
        if (isInitialized) return true;

        if (isInitializing)
        {
            Debug.LogWarning("[YooUtils] 正在初始化中，请稍候...");
            return false;
        }

        Debug.LogError("[YooUtils] 未初始化，请先调用 Initialize()");
        return false;
    }

    /// <summary>
    /// 检查是否已初始化
    /// </summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// 获取当前资源包
    /// </summary>
    public ResourcePackage GetPackage() => currentPackage;

    #endregion



    /// <summary>
    /// 获取 CDN URL（根据平台自动选择路径）
    /// </summary>
    private string GetHostServerURL(string baseUrl)
    {
        string platform = GetPlatformName();
        return $"{baseUrl}/{platform}/{settings.appVersion}";
    }

    private string GetPlatformName()
    {
#if UNITY_EDITOR
        var buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
        if (buildTarget == UnityEditor.BuildTarget.Android)
            return "Android";
        else if (buildTarget == UnityEditor.BuildTarget.iOS)
            return "iOS";
        else if (buildTarget == UnityEditor.BuildTarget.WebGL)
            return "WebGL";
        else
            return "PC";
#else
        if (Application.platform == RuntimePlatform.Android)
            return "Android";
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
            return "iOS";
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
            return "WebGL";
        else
            return "PC";
#endif
    }

    /// <summary>
    /// 测试网络连接
    /// </summary>
    private IEnumerator TestNetworkConnection(string cdnBaseUrl, System.Action<bool> callback)
    {
        // 尝试访问 NetworkVerifiedAssetName 文件来验证网络连接
        string testUrl = $"{cdnBaseUrl}/{settings.networkVerifiedAssetName}";
        Debug.Log($"测试连接: {testUrl}");

        using (UnityWebRequest request = UnityWebRequest.Head(testUrl))
        {
            // 设置超时时间（5秒）
            request.timeout = 5;
            yield return request.SendWebRequest();

            bool success = false;
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✓ 网络连接成功: {testUrl} (状态码: {request.responseCode})");
                success = true;
            }
            else if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogWarning($"✗ 连接错误: {testUrl} - {request.error}");
                success = false;
            }
            else if (request.result == UnityWebRequest.Result.ProtocolError)
            {
                // 404 也算连接成功（至少能访问到服务器）
                if (request.responseCode == 404)
                {
                    Debug.LogWarning($"⚠ 文件不存在 (404): {testUrl}，但网络连接正常");
                    success = true;
                }
                else
                {
                    Debug.LogWarning($"✗ 协议错误: {testUrl} - {request.responseCode} {request.error}");
                    success = false;
                }
            }
            else
            {
                Debug.LogWarning($"✗ 未知错误: {testUrl} - {request.error}");
                success = false;
            }

            callback?.Invoke(success);
        }
    }


    private void OnDestroy()
    {
        // 释放所有资源句柄
        ReleaseAllAssets();
    }

    /// <summary>
    /// 远端资源地址查询服务类
    /// </summary>
    private class RemoteServices : IRemoteServices
    {
        private readonly string _defaultHostServer;
        private readonly string _fallbackHostServer;

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
