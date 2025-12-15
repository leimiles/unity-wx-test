using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.Networking;

public class YooUtilsByUniTask : ISubSystem
{
    /// <summary>
    /// 子系统名称
    /// </summary>
    public string Name => "YooUtils";
    public int Priority => 1;
    /// <summary>
    /// 是否必须
    /// </summary>
    public bool IsRequired => true;
    /// <summary>
    /// 进度
    /// </summary>
    public float Progress => isInitialized ? 1.0f : 0.0f;
    /// <summary>
    /// 是否初始化完成
    /// </summary>
    public bool IsInitialized => isInitialized;
    readonly YooUtilsSettings settings;
    bool isInitialized = false;
    ResourcePackage currentPackage;
    public event Action OnInitialized;
    public event Action<string> OnInitializeFailed;
    UniTask? _initTask;
    Dictionary<string, AssetHandleInfo> activeHandles = new();

    class AssetHandleInfo
    {
        public AssetHandle Handle { get; set; }
        public AssetHandleInfo(AssetHandle handle)
        {
            Handle = handle;
        }
    }

    public YooUtilsByUniTask(YooUtilsSettings yooUtilsSettings)
    {
        settings = yooUtilsSettings;
    }

    public UniTask InitializeAsync(CancellationToken ct = default)
    {
        if (isInitialized) return UniTask.CompletedTask;

        if (!_initTask.HasValue)
        {
            // 第一次调用：启动初始化（使用内部的 CTS，不受外部取消影响）
            // 初始化过程一旦开始就不应该被取消
            _initTask = InitializeInternalAsync();
        }

        // 所有调用都支持外部取消等待（但不取消初始化本身）
        // 这样调用者可以取消等待，避免阻塞，但初始化会继续完成
        return _initTask.Value.AttachExternalCancellation(ct);
    }

    async UniTask InitializeInternalAsync()
    {
        try
        {
            // verify settings
            if (settings == null)
            {
                string error = "YooUtilsSettings is not set, please set it in the inspector";
                Debug.LogError(error);
                throw new InvalidOperationException(error);
            }

            Debug.Log("=== Start YooAsset initialization ===");

            // 1. Initialize YooAsset
            if (!YooAssets.Initialized)
            {
                Debug.Log("Step 1: Initialize YooAsset...");
                YooAssets.Initialize();
            }

            // 2. Create package
            Debug.Log($"Step 2: Create package '{settings.packageName}'...");
            currentPackage = YooAssets.TryGetPackage(settings.packageName);
            currentPackage ??= YooAssets.CreatePackage(settings.packageName);

            // 3. Configure CDN address
            Debug.Log($"Step 3: Configure CDN address '{settings.hostServerURL}'...");
            string defaultHostServer = GetHostServerURL(settings.hostServerURL);
            string fallbackHostServer = string.IsNullOrEmpty(settings.hostServerFallbackURL) ? defaultHostServer : GetHostServerURL(settings.hostServerFallbackURL);
            IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            Debug.Log($"Main CDN address: {defaultHostServer}");
            Debug.Log($"Fallback CDN address: {fallbackHostServer}");

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

            // 4. Initialize resource package
            Debug.Log("Step 4: Initialize resource package...");
            InitializationOperation initOperation = null;

            switch (settings.playMode)
            {
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
                case EPlayMode.CustomPlayMode:
                    string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/_GAME_FILE_CACHE/yoo";
                    var createParametersCustom = new WebPlayModeParameters
                    {
                        WebServerFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices, null)
                    };
                    initOperation = currentPackage.InitializeAsync(createParametersCustom);
                    break;
                default:
                    throw new System.Exception($"Unsupported play mode: {settings.playMode}");
            }

            await initOperation.ToUniTask();

            // Check initialization result
            if (initOperation.Status != EOperationStatus.Succeed)
            {
                throw new System.Exception($"Resource package initialization failed: {initOperation.Error}");
            }

            Debug.Log("Resource package initialization succeeded");

            // 5. Request package version
            string packageVersion = null;
            if (settings.playMode == EPlayMode.HostPlayMode || settings.playMode == EPlayMode.WebPlayMode || settings.playMode == EPlayMode.CustomPlayMode)
            {
                Debug.Log("Step 5: Request package version...");
                var versionOperation = currentPackage.RequestPackageVersionAsync(false);
                await versionOperation.ToUniTask();

                if (versionOperation.Status != EOperationStatus.Succeed)
                {
                    throw new System.Exception($"Request package version failed: {versionOperation.Error}");
                }

                packageVersion = versionOperation.PackageVersion;
                Debug.Log($"Package version: {packageVersion}");

                // 6. Update package manifest
                Debug.Log("Step 6: Update package manifest...");
                var manifestOperation = currentPackage.UpdatePackageManifestAsync(packageVersion);
                await manifestOperation.ToUniTask();

                if (manifestOperation.Status != EOperationStatus.Succeed)
                {
                    throw new System.Exception($"Update package manifest failed: {manifestOperation.Error}");
                }

                Debug.Log("Package manifest updated successfully");
            }

            // 7. Set default package
            YooAssets.SetDefaultPackage(currentPackage);
            Debug.Log("Default package set successfully");

            isInitialized = true;
            Debug.Log("=== YooAsset initialization completed ===");
            OnInitialized?.Invoke();

        }
        catch (Exception e)
        {
            Debug.LogError($"YooUtils initialization failed: {e}");
            OnInitializeFailed?.Invoke(e.Message);
            throw;
        }
        finally
        {
            if (!isInitialized)
            {
                // 初始化失败，允许重试
                _initTask = null;
            }
        }

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
