using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WXSettings", menuName = "Generate/WXSettings", order = 1)]
public class WXSettings : ScriptableObject
{
    public string appId = "your_app_id_here";
    public string cdnUrl = "https://your.cdn.url/here/";
    public string appName = "YourAppName";
}
