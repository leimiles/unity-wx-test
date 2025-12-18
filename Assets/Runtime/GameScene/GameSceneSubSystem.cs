using Cysharp.Threading.Tasks;
using System;

public class GameSceneSubSystem : ISubSystem
{
    private readonly IGameSceneService _gameSceneService;

    public string Name => "GameSceneSubSystem";
    public int Priority => 2;
    public bool IsRequired => true;
    public bool IsInitialized => _gameSceneService.IsInitialized;

    public GameSceneSubSystem(IYooService yooService)
    {
        if (yooService == null)
            throw new ArgumentNullException(nameof(yooService));

        _gameSceneService = new GameSceneService(yooService);
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