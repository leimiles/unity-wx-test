using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WXSettings", menuName = "Generate/WXSettings", order = 1)]
public class WXSettings : ScriptableObject
{
    public enum FirstBundleLoadingMethodEnum
    {
        CDN,
        Local,
    }
    public string appId = "wxc3fc029bfde9435b";
    public string cdnUrl = "https://a.unity.cn/client_api/v1/buckets/be845f55-2cfe-4e08-87e9-1b8162181a67/release_by_badge/dev_01_h/content";
    public string appName = "朱麦历险记";
    public FirstBundleLoadingMethodEnum firstBundleLoadingMethod = FirstBundleLoadingMethodEnum.CDN;
    public int firstBundleCompress = 1;
    public int preloadWXFont = 0;
    public int WXMultiTouchForbidden = 1;

}
