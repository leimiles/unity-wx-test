using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IGameServices
{
    /// <summary>
    /// 注册服务
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="instance"></param>
    void Register<T>(T instance) where T : class;
    /// <summary>
    /// 尝试获取服务
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="instance"></param>
    /// <returns></returns>
    bool TryGet<T>(out T instance) where T : class;
    /// <summary>
    /// 获取服务
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T Get<T>() where T : class;
    /// <summary>
    /// 是否已注册
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    bool IsRegistered<T>() where T : class;
}

public sealed class GameServices : IGameServices
{
    private readonly Dictionary<Type, object> _map = new();
    private readonly object _gate = new();

    public void Register<T>(T instance) where T : class
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        var type = typeof(T);

        lock (_gate)
        {
            if (_map.ContainsKey(type))
                throw new InvalidOperationException($"Service already registered: {type.FullName}");

            _map[type] = instance;
        }
    }

    public bool TryGet<T>(out T instance) where T : class
    {
        lock (_gate)
        {
            if (_map.TryGetValue(typeof(T), out var obj) && obj is T cast)
            {
                instance = cast;
                return true;
            }
        }
        instance = null;
        return false;
    }

    public T Get<T>() where T : class
    {
        if (TryGet<T>(out var instance))
            return instance;

        throw new KeyNotFoundException(
            $"Service not registered: {typeof(T).FullName}. " +
            $"Make sure it is registered in GameManager before use."
        );
    }

    public bool IsRegistered<T>() where T : class => TryGet<T>(out _);
}
