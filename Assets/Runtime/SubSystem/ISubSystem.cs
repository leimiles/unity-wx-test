using Cysharp.Threading.Tasks;
using System;
/// <summary>
/// 子系统接口
/// </summary>
public interface ISubSystem
{
    public string Name { get; }
    public int Priority { get; }
    public bool IsRequired { get; }
    // public float Progress { get; }
    public bool IsInitialized { get; }
    public UniTask InitializeAsync(IProgress<float> progress);
    public void Dispose();
}
