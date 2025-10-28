using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeChatWASM;

public class Perf : MonoBehaviour
{
    void Start()
    {
        ReportSceneOption option = new ReportSceneOption();
        option.sceneId = 1000;

        Debug.Log("option: " + option.sceneId);


    }
}
