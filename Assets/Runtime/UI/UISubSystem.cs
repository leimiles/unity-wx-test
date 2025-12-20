using Cysharp.Threading.Tasks;
using System;

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

    public void Dispose()
    {
    }
}
