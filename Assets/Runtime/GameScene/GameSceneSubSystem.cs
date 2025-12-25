using Cysharp.Threading.Tasks;
using System;

public class GameSceneSubSystem : ISubSystem
{
    public string Name => "GameSceneSubSystem";
    public int Priority => 2;
    public bool IsRequired => true;
    public bool IsReady => _gameSceneService != null && _gameSceneService.IsInitialized;
    public bool IsInstalled => _installed;
    bool _installed = false;
    IGameSceneService _gameSceneService;
    readonly GameSceneSettings _gameSceneSettings;
    readonly IGameServices _gameServices;

    public GameSceneSubSystem(BootstrapConfigs configs, IGameServices gameServices)
    {
        if (configs == null)
        {
            throw new ArgumentNullException(nameof(configs));
        }
        if (configs.gameSceneSettings == null)
        {
            throw new ArgumentNullException(nameof(configs.gameSceneSettings));
        }
        _gameSceneSettings = configs.gameSceneSettings;
        _gameServices = gameServices ?? throw new ArgumentNullException(nameof(gameServices));
    }
    public void Install(IGameServices services)
    {
        if (_installed) return;
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        if (_gameSceneService == null)
        {
            throw new InvalidOperationException(
                "GameSceneService is not initialized before Install"
            );
        }

        services.Register<IGameSceneService>(_gameSceneService);
        _installed = true;
    }

    public UniTask InitializeAsync(IProgress<float> progress)
    {
        var yooService = _gameServices.Get<IYooService>();
        if (yooService == null)
        {
            throw new InvalidOperationException(
                "YooService is not initialized before InitializeAsync"
            );
        }
        _gameSceneService = new GameSceneService(yooService);
        return _gameSceneService.InitializeAsync(progress);
    }

    public void Dispose()
    {
        _gameSceneService?.Dispose();
    }
}