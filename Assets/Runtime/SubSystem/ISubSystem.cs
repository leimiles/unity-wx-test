using Cysharp.Threading.Tasks;
using System;
/// <summary>
/// 子系统接口
/// </summary>
public interface ISubSystem
{
    /// <summary>
    /// 子系统名称
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// 子系统优先级
    /// </summary>
    public int Priority { get; }
    /// <summary>
    /// 子系统是否必需
    /// </summary>
    public bool IsRequired { get; }
    /// <summary>
    /// 子系统是否准备好
    /// </summary>
    public bool IsReady { get; }
    /// <summary>
    /// 子系统是否已安装
    /// </summary>
    public bool IsInstalled { get; }
    /// <summary>
    /// 安装子系统
    /// </summary>
    /// <param name="services"></param>
    public void Install(IGameServices services);
    /// <summary>
    /// 初始化子系统
    /// </summary>
    /// <param name="progress"></param>
    /// <returns></returns>
    public UniTask InitializeAsync(IProgress<float> progress);
    /// <summary>
    /// 释放子系统中的资源
    /// </summary>
    public void Dispose();
}
