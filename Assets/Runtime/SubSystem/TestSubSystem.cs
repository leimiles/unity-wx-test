using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

/// <summary>
/// 测试用的第二个 SubSystem
/// </summary>
public class TestSubSystem : ISubSystem
{
    public string Name => "TestSystem";
    public int Priority => 2; // 优先级比 YooUtils 低，会在后面初始化
    public bool IsRequired => false; // 设为非必需，测试 Optional 系统
    public bool IsInitialized { get; private set; }

    public async UniTask InitializeAsync(IProgress<float> progress)
    {
        Debug.Log($"[TestSubSystem] 开始初始化...");

        // 模拟初始化过程
        for (int i = 0; i <= 10; i++)
        {
            await UniTask.Delay(100); // 模拟异步操作
            progress?.Report(i / 10f);
            Debug.Log($"[TestSubSystem] 初始化进度: {i * 10}%");
        }

        IsInitialized = true;
        Debug.Log($"[TestSubSystem] 初始化完成！");
    }
}