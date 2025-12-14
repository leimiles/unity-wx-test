using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameScene
{
    string Name { get; }
}

public enum GameSceneType
{
    MainMenu,
    Loading,
    GameLevel
}
