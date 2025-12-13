using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

[DisallowMultipleComponent]
public class Bootstrap : MonoBehaviour
{
    [SerializeField] int frameRate = 60;
    [SerializeField] bool runInBackground = true;
    public EPlayMode playMode = EPlayMode.EditorSimulateMode;

    void Awake()
    {
        Application.targetFrameRate = frameRate;
        Application.runInBackground = runInBackground;
        DontDestroyOnLoad(this.gameObject);
    }

    IEnumerator Start()
    {
        // GameManager 启动

        // 事件系统

        // YooaAssets 资源系统启动

        // 补丁更新，暂时没有
        yield return null;

        // 设置资源包

        // 加载主场景


    }
}
