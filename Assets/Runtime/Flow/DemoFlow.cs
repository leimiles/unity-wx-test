using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine;

public class DemoFlow : IGameFlow
{
    readonly IGameServices _services;
    public DemoFlow(IGameServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }
    public async UniTask RunAsync(CancellationToken ct)
    {
        var sceneService = _services.Get<IGameSceneService>();

        var yooService = _services.Get<IYooService>();

        var assetInfo = yooService.GetAssetInfo("Main");

        if (assetInfo != null)
        {
            Debug.Log($"assetInfo: <color=red>{assetInfo.Error}</color>");
        }
        //await sceneService.LoadSceneAsync("Main").AttachExternalCancellation(ct);
    }
}
