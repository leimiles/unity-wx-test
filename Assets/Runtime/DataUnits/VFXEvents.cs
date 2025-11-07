using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 资源分页切换或资源分页可见范围变化时（滚动）触发的事件
/// 触发时会进行资源的预加载
/// </summary>
public struct VFXPageVisibleRangeChangedEvent : IEvent
{
    public int pageIndex; // 分页索引
    public int visibleStartIndex; // 可见范围起始索引
    public int visibleCount; // 可见范围数量
    public int preloadCount; // 预加载数量
    public int loadStartIndex; // 加载起始索引
    public string[] bundleAddresses; // 资源包地址
    public string[] assetNames; // 资源名称
    public string[] unitIDs; // 所有单元ID
}

/// <summary>
/// VFX 资源预加载进度事件
/// 用于显示加载进度
/// </summary>
public struct VFXPreloadProgressEvent : IEvent
{

    public int loadedCount; // 已加载数量
    public int totalCount;  // 需要加载的总数量
    public float currentBundleProgress; // 当前资源包加载进度
    public float totlaDownloadProgress; // 总下载进度
    public string currentLoadingUnitID; // 当前加载单元ID
}

/// <summary>
/// VFX 资源预加载完成事件
/// 用于通知资源预加载完成
/// </summary>
public struct VFXPreloadCompletedEvent : IEvent
{
    public int pageIndex; // 分页索引
    public bool success; // 是否成功
    public string errorMessage; // 错误信息
}

/// <summary>
/// VFX 单元加载完成事件
/// 用于通知单元加载完成
/// </summary>
public struct VFXUnitLoadedCompletedEvent : IEvent
{
    public string unitID;
    public bool success;
    public string errorMessage;
}

/// <summary>
/// VFX 图标点击事件
/// 用于通知图标点击事件
/// </summary>
public struct VFXIconClickedEvent : IEvent
{
    public string unitID;
    public Vector3? spawnPosition;
    public Transform parentTransform;
}





