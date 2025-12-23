using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public class TestUIFlow : IGameFlow
{
    readonly IGameServices _services;
    public TestUIFlow(IGameServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }
    public async UniTask RunAsync(CancellationToken ct)
    {
        // todo: ui service works here
        Debug.Log("TestUIFlow start");
        await UniTask.CompletedTask;
    }
}
