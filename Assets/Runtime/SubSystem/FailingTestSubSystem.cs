using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class FailingTestSubSystem : ISubSystem
{
    public string Name => "FailingTestSystem";
    public int Priority => 3;
    public bool IsRequired => false; // Optional
    public bool IsInitialized => false;

    public async UniTask InitializeAsync(IProgress<float> progress)
    {
        await UniTask.Delay(500);
        progress?.Report(0.5f);
        throw new Exception("测试失败：Optional 系统失败不应该中断启动流程");
    }

    public void Dispose()
    {
        Debug.Log($"[FailingTestSubSystem] 资源已释放");
        // 在这里添加释放资源的逻辑
    }
}