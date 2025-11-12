using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeChatWASM;

public class Perf : MonoBehaviour
{
    void Start()
    {
        WX.ReportGameStart();
    }
}
