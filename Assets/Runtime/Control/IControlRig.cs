using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IControlRig
{
    /// <summary>
    /// 是否已附加
    /// </summary>
    bool IsAttached { get; }
    /// <summary>
    /// 附加
    /// </summary>
    void Attach();
    /// <summary>
    /// 分离
    /// </summary>
    void Detach();
    /// <summary>
    /// 重置
    /// </summary>
    void Reset();
}
