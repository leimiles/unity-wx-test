using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WXBuildSettings", menuName = "WX/Build Settings", order = 1)]
public class WXBuildSettings : ScriptableObject
{
    public enum FirstBundleLoadingMethodEnum
    {
        CDN,
        Local,
    }

    [Tooltip("微信小游戏AppID")]
    public string appId = "";

    [Tooltip("CDN地址")]
    [TextArea(2, 4)]
    public string cdnURL = "https://a.unity.cn/client_api/v1/buckets/be845f55-2cfe-4e08-87e9-1b8162181a67/release_by_badge/dev_01_h/content";

    [Tooltip("应用名称")]
    public string appName = "朱麦历险记";

    [Tooltip("首包加载方式")]
    public FirstBundleLoadingMethodEnum firstBundleLoadingMethod = FirstBundleLoadingMethodEnum.CDN;

    [Tooltip("首包是否压缩")]
    public bool firstBundleCompress = true;

    [Tooltip("是否预加载微信字体")]
    public bool preloadWXFont = false;

    [Tooltip("是否禁止多点触控")]
    public bool WXMultiTouchForbidden = true;

    [Tooltip("是否开启 development build")]
    public bool devbuild = false;

    [Tooltip("是否开启自动连接性能分析器")]
    public bool autoConnnectProfiler = false;

    [Tooltip("是否开启 il2cpp 优化")]
    public bool il2CppOptimization = true;

    [Tooltip("是否开启性能分析")]
    public bool profilingFunc = false;

    [Tooltip("是否开启内存分析")]
    public bool profilingMemory = false;

    [Tooltip("是否开启 WebGL2")]
    public bool webGL2 = true;

    [Tooltip("是否开启 iOS 性能增强")]
    public bool iOSPerformancePlus = true;

    [Tooltip("是否开启 Emscripten GLX 支持")]
    public bool emscriptenGLX = true;

    [Tooltip("是否清理 StreamingAssets")]
    public bool clearStreamingAsset = true;

    [Tooltip("是否清理 WebGL 构建产物")]
    public bool cleanWebGLBuild = false;

    [Tooltip("首包资源优化")]
    public bool firstBundleOptimization = true;

    [Tooltip("适配屏幕尺寸")]
    public bool adaptedScreenSize = true;

    [Tooltip("显示优化提示")]
    public bool showOptTips = false;

    [Tooltip("显示性能面板")]
    public bool showPerfPanel = false;

    [Tooltip("显示渲染日志")]
    public bool showRenderingLog = false;

    [Tooltip("Brotli压缩")]
    public bool brotliCompress = true;

    // 便捷属性
    public bool IsFirstBundleFromCDN => firstBundleLoadingMethod == FirstBundleLoadingMethodEnum.CDN;

    private void OnValidate()
    {
        // 清理默认示例 AppID，避免误提交真实 ID
        if (appId != null && appId.Trim() == "wxc3fc029bfde9435b")
        {
            Debug.LogWarning($"WXBuildSettings: 当前 appId 为示例值，建议替换为实际 AppID 或留空以避免误提交.");
        }

        // 简单 URL 校验
        if (!string.IsNullOrWhiteSpace(cdnURL) && !(cdnURL.StartsWith("http://") || cdnURL.StartsWith("https://")))
        {
            Debug.LogWarning($"WXBuildSettings: cdnURL 看起来不是合法的 URL: {cdnURL}");
        }

        // 保证 appName 不为空（不强制，只警告）
        if (string.IsNullOrWhiteSpace(appName))
        {
            Debug.LogWarning("WXBuildSettings: appName 为空，建议填写人可读的应用名称。");
        }

#if UNITY_EDITOR
        // 标记为脏，以便更改生效并保存
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
