using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSceneSettings", menuName = "WX/Configs/GameSceneSettings", order = 1)]
public class GameSceneSettings : IConfigs
{
    [Header("游戏场景配置")]
    [Tooltip("第一个场景名称")]
    public string firstSceneName = "MainMenu";
}
