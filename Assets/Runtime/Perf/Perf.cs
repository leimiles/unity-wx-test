using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_WEBGL
using WeChatWASM;
#endif

public class Perf : MonoBehaviour
{
    void Start()
    {
#if UNITY_WEBGL
        WX.ReportGameStart();
#endif
    }
}
