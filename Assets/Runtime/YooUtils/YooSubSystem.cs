using Cysharp.Threading.Tasks;
using System;

public class YooSubSystem : ISubSystem
{
    public string Name => "YooSubSystem";
    public int Priority => 1;
    public bool IsRequired => true;
    public bool IsReady => _yooService != null && _yooService.IsInitialized;
    IYooService _yooService;
    readonly YooSettings _yooSettings;
    public bool IsInstalled => _installed;
    bool _installed = false;

    public YooSubSystem(BootstrapConfigs configs)
    {
        if (configs == null)
        {
            throw new ArgumentNullException(nameof(configs));
        }
        if (configs.yooSettings == null)
        {
            throw new ArgumentNullException(nameof(configs.yooSettings));
        }
        _yooSettings = configs.yooSettings;
    }

    public void Install(IGameServices services)
    {
        if (_installed) return;

        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (_yooService == null)
            throw new InvalidOperationException("YooService not initialized");

        services.Register<IYooService>(_yooService);
        _installed = true;
    }


    public UniTask InitializeAsync(IProgress<float> progress)
    {
        _yooService = new YooService(_yooSettings);
        return _yooService.InitializeAsync(progress);
    }

    public void Dispose()
    {
        _yooService.Dispose();
    }

}
