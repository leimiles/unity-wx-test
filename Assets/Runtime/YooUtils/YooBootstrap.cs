using UnityEngine;
using YooAsset;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// YooAsset CDN 资源加载最小验证
/// </summary>
public class YooBootstrap : MonoBehaviour
{
    [Header("CDN 配置")]
    [Tooltip("CDN 服务器地址（例如：https://cdn.example.com）")]
    public string cdnUrl = "https://your-cdn-server.com";

    [Tooltip("资源版本号（例如：v1.0）")]
    public string appVersion = "v1.0";

    [Tooltip("资源包名称")]
    public string packageName = "DefaultPackage";

    [Header("测试配置")]
    [Tooltip("用于验证的资源名称（需要确保该资源在 CDN 上存在）")]
    public string testAssetName = "JiJiGuoWang@Suit_580";

    [Tooltip("运行模式")]
    public EPlayMode playMode = EPlayMode.HostPlayMode;

    private void Start()
    {
        StartCoroutine(InitializeAndVerify());
    }

    private IEnumerator InitializeAndVerify()
    {
        Debug.Log("=== 开始 YooAsset CDN 验证 ===");

        // 1. 初始化 YooAsset
        Debug.Log("步骤 1: 初始化 YooAsset...");
        YooAssets.Initialize();

        // 2. 创建资源包
        Debug.Log($"步骤 2: 创建资源包 '{packageName}'...");
        var package = YooAssets.TryGetPackage(packageName);
        if (package == null)
        {
            package = YooAssets.CreatePackage(packageName);
        }

        // 3. 配置 CDN 地址
        Debug.Log($"步骤 3: 配置 CDN 地址 '{cdnUrl}'...");
        string defaultHostServer = GetCDNUrl();
        string fallbackHostServer = GetCDNUrl(); // 备用地址，可以设置为不同的 CDN
        IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
        Debug.Log($"主 CDN 地址: {defaultHostServer}");
        Debug.Log($"备用 CDN 地址: {fallbackHostServer}");

        // 3.5. 验证网络连接
        Debug.Log("步骤 3.5: 验证网络连接...");
        bool networkAvailable = false;
        yield return StartCoroutine(TestNetworkConnection(defaultHostServer, (result) => networkAvailable = result));

        if (!networkAvailable)
        {
            Debug.LogWarning("主 CDN 连接失败，尝试备用 CDN...");
            yield return StartCoroutine(TestNetworkConnection(fallbackHostServer, (result) => networkAvailable = result));
        }

        if (!networkAvailable)
        {
            Debug.LogError("✗ 网络连接验证失败！无法访问 CDN 服务器");
            Debug.LogError($"请检查：");
            Debug.LogError($"1. CDN 地址是否正确: {cdnUrl}");
            Debug.LogError($"2. 网络连接是否正常");
            Debug.LogError($"3. CDN 服务器是否可访问");
            yield break;
        }

        // 4. 初始化资源包
        Debug.Log("步骤 4: 初始化资源包...");
        InitializationOperation initOperation = null;

        if (playMode == EPlayMode.HostPlayMode)
        {
            var createParameters = new HostPlayModeParameters();
            createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            createParameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
            initOperation = package.InitializeAsync(createParameters);
        }
        else if (playMode == EPlayMode.WebPlayMode)
        {
            var createParameters = new WebPlayModeParameters();
            createParameters.WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
            initOperation = package.InitializeAsync(createParameters);
        }
        else
        {
            Debug.LogError($"不支持的运行模式: {playMode}，请使用 HostPlayMode 或 WebPlayMode");
            yield break;
        }

        yield return initOperation;

        // 5. 检查初始化结果
        if (initOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"资源包初始化失败: {initOperation.Error}");
            yield break;
        }

        Debug.Log("✓ 资源包初始化成功！");

        // 6. 请求资源版本（HostPlayMode 和 WebPlayMode 需要）
        string packageVersion = null;
        if (playMode == EPlayMode.HostPlayMode || playMode == EPlayMode.WebPlayMode)
        {
            Debug.Log("步骤 5: 请求资源版本...");
            var versionOperation = package.RequestPackageVersionAsync();
            yield return versionOperation;

            if (versionOperation.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"请求资源版本失败: {versionOperation.Error}");
                Debug.LogError("请确保 CDN 服务器上存在 PackageVersion.txt 文件");
                yield break;
            }

            packageVersion = versionOperation.PackageVersion;
            Debug.Log($"✓ 获取到资源版本: {packageVersion}");
        }

        // 7. 更新资源清单（HostPlayMode 和 WebPlayMode 需要）
        if (playMode == EPlayMode.HostPlayMode || playMode == EPlayMode.WebPlayMode)
        {
            Debug.Log("步骤 6: 更新资源清单...");
            var manifestOperation = package.UpdatePackageManifestAsync(packageVersion);
            yield return manifestOperation;

            if (manifestOperation.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"更新资源清单失败: {manifestOperation.Error}");
                Debug.LogError("请确保 CDN 服务器上存在对应的 manifest 文件");
                yield break;
            }

            Debug.Log("✓ 资源清单更新成功！");
        }

        // 8. 设置默认资源包
        YooAssets.SetDefaultPackage(package);
        Debug.Log("✓ 已设置默认资源包");

        // 9. 尝试加载测试资源进行验证
        Debug.Log($"步骤 7: 尝试从 CDN 加载测试资源 '{testAssetName}'...");
        var assetHandle = YooAssets.LoadAssetAsync<GameObject>(testAssetName);
        yield return assetHandle;

        // 10. 验证加载结果
        if (assetHandle.Status == EOperationStatus.Succeed)
        {
            Debug.Log($"✓✓✓ 验证成功！成功从 CDN 加载资源: {testAssetName}");
            Debug.Log($"资源类型: {assetHandle.AssetObject.GetType().Name}");

            // 可以在这里实例化资源进行进一步验证
            //var instance = assetHandle.InstantiateSync();
            //Debug.Log($"资源实例化成功: {instance.name}");

            // 释放资源句柄
            assetHandle.Release();
        }
        else
        {
            Debug.LogError($"✗✗✗ 验证失败！无法从 CDN 加载资源: {testAssetName}");
            Debug.LogError($"错误信息: {assetHandle.LastError}");
        }

        Debug.Log("=== YooAsset CDN 验证完成 ===");
    }

    /// <summary>
    /// 测试网络连接
    /// </summary>
    private IEnumerator TestNetworkConnection(string cdnBaseUrl, System.Action<bool> callback)
    {
        // 尝试访问 PackageVersion.txt 文件来验证网络连接
        string testUrl = $"{cdnBaseUrl}/DefaultPackage.version";
        Debug.Log($"测试连接: {testUrl}");

        using (UnityWebRequest request = UnityWebRequest.Head(testUrl))
        {
            // 设置超时时间（5秒）
            request.timeout = 5;
            yield return request.SendWebRequest();

            bool success = false;
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✓ 网络连接成功: <color=green>{testUrl}</color> (状态码: {request.responseCode})");
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
                    Debug.LogWarning($"⚠ 文件不存在 (404): <color=red>{testUrl}</color>，但网络连接正常");
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

    /// <summary>
    /// 获取 CDN URL（根据平台自动选择路径）
    /// </summary>
    private string GetCDNUrl()
    {
        string platform = GetPlatformName();
        return $"{cdnUrl}/{platform}/{appVersion}";
    }

    /// <summary>
    /// 获取平台名称
    /// </summary>
    private string GetPlatformName()
    {
#if UNITY_EDITOR
        var buildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
        if (buildTarget == UnityEditor.BuildTarget.Android)
            return "Android";
        else if (buildTarget == UnityEditor.BuildTarget.iOS)
            return "iPhone";
        else if (buildTarget == UnityEditor.BuildTarget.WebGL)
            return "WebGL";
        else
            return "PC";
#else
        if (Application.platform == RuntimePlatform.Android)
            return "Android";
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
            return "iPhone";
        else if (Application.platform == RuntimePlatform.WebGLPlayer)
            return "WebGL";
        else
            return "PC";
#endif
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
            string url = $"{_defaultHostServer}/{fileName}";
            Debug.Log($"主 CDN 地址: {url}");
            return url;
        }

        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            string url = $"{_fallbackHostServer}/{fileName}";
            Debug.Log($"备用 CDN 地址: {url}");
            return url;
        }
    }
}