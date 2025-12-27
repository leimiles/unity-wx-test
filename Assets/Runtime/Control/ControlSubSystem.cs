using Cysharp.Threading.Tasks;
using System;

public class ControlSubSystem : ISubSystem
{
    public string Name => "ControlSubSystem";
    public int Priority => 5;
    public bool IsRequired => true;
    public bool IsReady => _controlService != null;
    public bool IsInstalled => _installed;
    bool _installed = false;
    IControlService _controlService;

    public UniTask InitializeAsync(IProgress<float> progress)
    {
        _controlService = new ControlService();
        progress?.Report(1f);
        return UniTask.CompletedTask;
    }

    public void Install(IGameServices services)
    {
        if (_installed) return;
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        services.Register<IControlService>(_controlService);
        _installed = true;
    }
    public void Dispose()
    {
        // 如果需要清理，在这里实现
    }
}

