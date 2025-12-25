using Cysharp.Threading.Tasks;
using System;

public sealed class UISubSystem : ISubSystem
{
    public string Name => "UI";
    public int Priority => 99;
    public bool IsRequired => true;
    public bool IsReady => false;
    public bool IsInstalled => _installed;
    bool _installed = false;
    public void Install(IGameServices services)
    {
        if (_installed) return;
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        _installed = true;
    }
    public UniTask InitializeAsync(IProgress<float> progress)
    {
        return UniTask.CompletedTask;
    }

    public void Dispose()
    {
    }
}
