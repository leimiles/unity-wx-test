using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeChatWASM;

[DisallowMultipleComponent]
public class Devices : MonoBehaviour
{
    void Start()
    {
        Debug.Log("获取设备信息");
        WX.GetSystemInfo(new GetSystemInfoOption()
        {
            success = (res) =>
            {
                Debug.Log($"设备型号: {res.model}");
                Debug.Log($"屏幕宽度: {res.screenWidth}px");
                Debug.Log($"系统版本: {res.system}");
            },
            fail = (res) =>
            {
                Debug.LogError($"获取设备信息失败: {res.errMsg}");
            }
        });
    }
}