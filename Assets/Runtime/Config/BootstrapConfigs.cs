using UnityEngine;

[CreateAssetMenu(fileName = "BootstrapConfigs", menuName = "WX/Configs/BootstrapConfigs", order = 0)]
public class BootstrapConfigs : ScriptableObject
{
    [Header("子系统配置")]
    [Tooltip("YooUtils 资源系统配置")]
    public YooSettings yooSettings;

    [Tooltip("游戏场景系统配置")]
    public GameSceneSettings gameSceneSettings;

    //[Tooltip("输入系统配置（如果需要）")]
    // public InputSettings inputSettings;

    // 可以继续添加其他子系统的配置

    public void Validate()
    {
        if (yooSettings == null)
        {
            throw new System.Exception("YooSettings is not set");
        }
        if (gameSceneSettings == null)
        {
            throw new System.Exception("GameSceneSettings is not set");
        }
    }
}