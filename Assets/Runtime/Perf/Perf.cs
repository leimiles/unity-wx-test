using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_WEBGL
using WeChatWASM;
#endif

[DisallowMultipleComponent]
public class Perf : MonoBehaviour
{
    [SerializeField] ColdMemoryMaker coldMemoryMaker;
    void Start()
    {
#if UNITY_WEBGL
        WX.ReportGameStart();
#endif
    }

}
