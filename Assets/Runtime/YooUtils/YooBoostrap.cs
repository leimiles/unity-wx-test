using System.Collections;
using UnityEngine;
using YooAsset;

public class YooBootstrap : MonoBehaviour
{
    [Header("YooAsset")]
    public string packageName = "DefaultPackage";

    // 你的 CDN 地址，指向包含 DefaultPackage.version 的目录
    public string hostServer = "https://your.cdn.com/path/to/DefaultPackage";

    private ResourcePackage _package;

    private IEnumerator Start()
    {
        // 1. 初始化 YooAsset 框架
        YooAssets.Initialize();

        // 2. 获取或创建 Package，并设为默认包
        _package = YooAssets.TryGetPackage(packageName);
        if (_package == null)
        {
            _package = YooAssets.CreatePackage(packageName);
        }
        YooAssets.SetDefaultPackage(_package);

        // 3. 配置 HostPlayMode 参数
        // 创建远程服务接口实现
        IRemoteServices remoteServices = new RemoteServices(hostServer, hostServer);

        var parameters = new HostPlayModeParameters();
        // 设置内置文件系统参数（从 StreamingAssets 加载）
        parameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
        // 设置缓存文件系统参数（从 CDN 下载）
        parameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);

        // 4. 初始化 Package
        var initOp = _package.InitializeAsync(parameters);
        yield return initOp;
        if (initOp.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"[YooBootstrap] Initialize failed : {initOp.Error}");
            yield break;
        }
        Debug.Log("[YooBootstrap] Initialize succeed.");

        // 5. 获取最新版本号
        var versionOp = _package.RequestPackageVersionAsync();
        yield return versionOp;
        if (versionOp.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"[YooBootstrap] RequestPackageVersion failed : {versionOp.Error}");
            yield break;
        }
        string packageVersion = versionOp.PackageVersion;
        Debug.Log("[YooBootstrap] Package version : " + packageVersion);

        // 6. 更新清单
        var manifestOp = _package.UpdatePackageManifestAsync(packageVersion);
        yield return manifestOp;
        if (manifestOp.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"[YooBootstrap] UpdatePackageManifest failed : {manifestOp.Error}");
            yield break;
        }
        Debug.Log("[YooBootstrap] UpdatePackageManifest succeed.");

        // 到这里，YooAsset + CDN 已经 ready 了
        // 你可以切换到"角色测试场景"，或者直接在本场景放 UI 按钮
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
            // 确保 hostServer 末尾没有斜杠，然后拼接文件名
            string baseUrl = _defaultHostServer.TrimEnd('/');
            return $"{baseUrl}/{fileName}";
        }

        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            // 确保 hostServer 末尾没有斜杠，然后拼接文件名
            string baseUrl = _fallbackHostServer.TrimEnd('/');
            return $"{baseUrl}/{fileName}";
        }
    }
}
