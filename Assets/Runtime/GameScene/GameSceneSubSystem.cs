using Cysharp.Threading.Tasks;
using System;

public class GameSceneSubSystem : ISubSystem
{
    private readonly IGameSceneService _gameSceneService;

    public string Name => "GameSceneSubSystem";
    public int Priority => 2;
    public bool IsRequired => true;
    public bool IsInitialized => _gameSceneService.IsInitialized;

    public GameSceneSubSystem(IGameSceneService gameSceneService)
    {
        _gameSceneService = gameSceneService
            ?? throw new ArgumentNullException(nameof(gameSceneService));
    }

    public UniTask InitializeAsync(IProgress<float> progress)
    {
        return _gameSceneService.InitializeAsync(progress);
    }

    public void Dispose()
    {
        // 如果需要清理，在这里实现
    }
}