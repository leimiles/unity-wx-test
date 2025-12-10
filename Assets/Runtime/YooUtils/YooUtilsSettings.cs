using UnityEngine;

[CreateAssetMenu(fileName = "YooUtilsSettings", menuName = "WX/YooUtilsSettings", order = 1)]
public class YooUtilsSettings : ScriptableObject
{
    [Header("服务器配置")]
    [Tooltip("主服务器地址")]
    public string hostServerURL = "https://a.unity.cn/client_api/v1/buckets/be845f55-2cfe-4e08-87e9-1b8162181a67/content/UOS CDN";

    [Tooltip("备用服务器地址（如果为空则使用主服务器地址）")]
    public string hostServerFallbackURL = "";

    [Header("版本配置")]
    [Tooltip("应用版本")]
    public string appVersion = "v1.0";

    [Tooltip("资源包名称")]
    public string packageName = "DefaultPackage";

    [Header("网络验证")]
    [Tooltip("是否需要验证网络连接")]
    public bool needNetworkVerified = true;

    [Tooltip("验证网络连接的资源名称")]
    public string networkVerifiedAssetName = "DefaultPackage.version";

    [Header("日志配置")]
    [Tooltip("日志级别：0=无日志, 1=仅错误, 2=警告+错误, 3=全部日志")]
    [Range(0, 3)]
    public int logLevel = 3;
}
