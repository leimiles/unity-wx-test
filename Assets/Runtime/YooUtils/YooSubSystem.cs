using Cysharp.Threading.Tasks;
using System;

public class YooSubSystem : ISubSystem
{
    public string Name => "YooUtils";
    public int Priority => 1;
    public bool IsRequired => true;
    public bool IsInitialized => _yooService.IsInitialized;
    readonly IYooService _yooService;
    // anotherServices such as YooService2, YooService3, etc.
    public YooSubSystem(IYooService yooService)
    {
        _yooService = yooService ?? throw new ArgumentNullException(nameof(yooService));
    }

    public UniTask InitializeAsync(IProgress<float> progress)
    {
        return _yooService.InitializeAsync(progress);
    }

}
