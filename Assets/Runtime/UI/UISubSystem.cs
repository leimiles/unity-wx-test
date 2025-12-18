using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

public sealed class UISubSystem : ISubSystem
{
    public string Name => "UI";
    public int Priority => 2;
    public bool IsRequired => true;
    public bool IsInitialized => false;
    public UniTask InitializeAsync(IProgress<float> progress)
    {
        return UniTask.CompletedTask;
    }

    /// <summary>
    /// 特权方法，用于在启动时显示 Loading UI，不依赖于子系统初始化
    /// </summary>
    public static void ShowLoadingUI()
    {
        LoadingUIService.Instance.ShowLoadingUI();
    }

    public void Dispose()
    {
        // 释放 UI 相关资源
        LoadingUIService.Instance.Dispose();
    }
}
