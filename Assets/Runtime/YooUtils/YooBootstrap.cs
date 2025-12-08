using UnityEngine;
using YooAsset;
using System.Collections;

public class YooBootstrap : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(InitializeYooAsset());
    }

    IEnumerator InitializeYooAsset()
    {
        // 1. 初始化 YooAssets
        YooAssets.Initialize();

        // 2. 创建或获取资源包
        string packageName = "DefaultPackage";
        var package = YooAssets.TryGetPackage(packageName);
        if (package == null)
            package = YooAssets.CreatePackage(packageName);

        // 3. 配置 CDN 地址
        string cdnURL = "https://your-cdn-server.com/yooasset"; // 替换为您的 CDN 地址
        IRemoteServices remoteServices = new RemoteServices(cdnURL, cdnURL);

        // 4. 初始化参数（HostPlayMode 联机模式）
        var createParameters = new HostPlayModeParameters();
        createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
        createParameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);

        // 5. 执行初始化
        var initOperation = package.InitializeAsync(createParameters);
        yield return initOperation;

        if (initOperation.Status == EOperationStatus.Succeed)
        {
            Debug.Log("YooAsset 初始化成功");

            // 设置为默认包
            YooAssets.SetDefaultPackage(package);

            // 开始热更新流程
            yield return UpdatePackage(package);
        }
        else
        {
            Debug.LogError($"初始化失败: {initOperation.Error}");
        }
    }

    IEnumerator UpdatePackage(ResourcePackage package)
    {
        // 6. 请求资源版本
        var versionOperation = package.RequestPackageVersionAsync();
        yield return versionOperation;

        if (versionOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"请求版本失败: {versionOperation.Error}");
            yield break;
        }

        string packageVersion = versionOperation.PackageVersion;
        Debug.Log($"获取到版本: {packageVersion}");

        // 7. 更新资源清单
        var manifestOperation = package.UpdatePackageManifestAsync(packageVersion);
        yield return manifestOperation;

        if (manifestOperation.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"更新清单失败: {manifestOperation.Error}");
            yield break;
        }

        // 8. 创建下载器（可选：下载所有资源或指定资源）
        var downloader = package.CreateResourceDownloader(10, 3);

        if (downloader.TotalDownloadCount > 0)
        {
            Debug.Log($"需要下载 {downloader.TotalDownloadCount} 个文件，总大小: {downloader.TotalDownloadBytes} 字节");

            // 开始下载
            downloader.BeginDownload();
            yield return downloader;

            if (downloader.Status == EOperationStatus.Succeed)
            {
                Debug.Log("资源下载完成");
            }
            else
            {
                Debug.LogError($"下载失败: {downloader.Error}");
            }
        }
        else
        {
            Debug.Log("无需下载，资源已是最新");
        }
    }

    // 远端资源地址服务类
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