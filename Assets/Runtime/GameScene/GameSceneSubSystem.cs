using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MilesUtils;
using Cysharp.Threading.Tasks;
using System;

public class GameSceneManager : ISubSystem
{
    public string Name => "GameSceneSubSystem";
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
