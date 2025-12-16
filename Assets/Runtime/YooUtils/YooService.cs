using UnityEngine;
using YooAsset;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;


public interface IYooService
{
    bool IsInitialized { get; }
    UniTask InitializeAsync(IProgress<float> progress);
}

public sealed class YooService : IYooService
{
    private volatile bool _isInitialized;
    public bool IsInitialized => _isInitialized;

    readonly YooUtilsSettings settings;
    ResourcePackage currentPackage;
    private readonly object _initGate = new object();
    private UniTaskCompletionSource _initTcs;

    public YooService(YooUtilsSettings yooUtilsSettings)
    {
        settings = yooUtilsSettings != null ? yooUtilsSettings : throw new ArgumentNullException(nameof(yooUtilsSettings));
    }

    public UniTask InitializeAsync(IProgress<float> progress)
    {
        if (_isInitialized)
        {
            progress?.Report(1.0f);
            return UniTask.CompletedTask;
        }

        UniTaskCompletionSource tcs;
        bool needStart = false;

        lock (_initGate)
        {
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
                    string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/_GAME_FILE_CACHE/yoo";
                    var createParametersCustom = new WebPlayModeParameters
                    {
                        WebServerFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices, null)
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
            if (settings.playMode == EPlayMode.HostPlayMode || settings.playMode == EPlayMode.WebPlayMode || settings.playMode == EPlayMode.CustomPlayMode)
            {
                Debug.Log("Step 5: Request package version...");
                var versionOperation = currentPackage.RequestPackageVersionAsync(false);
                await versionOperation.ToUniTask();
                progress?.Report(0.65f);

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
                progress?.Report(0.85f);

                if (manifestOperation.Status != EOperationStatus.Succeed)
                {
                    throw new System.Exception($"Update package manifest failed: {manifestOperation.Error}");
                }

                Debug.Log("Package manifest updated successfully");
            }
            else
            {
                progress?.Report(0.85f);
            }

            // 7. Set default package
            YooAssets.SetDefaultPackage(currentPackage);
            Debug.Log("Default package set successfully");
            progress?.Report(0.9f);

            _isInitialized = true;
            progress?.Report(1.0f);
            Debug.Log("=== YooAsset initialization completed ===");
            //OnInitialized?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"YooUtils initialization failed: {e}");
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
