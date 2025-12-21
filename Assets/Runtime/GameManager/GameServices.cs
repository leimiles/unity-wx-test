using System.Collections.Generic;
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
    /// 注册服务，如果服务已存在，则替换
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="instance"></param>
    void RegisterOrReplace<T>(T instance) where T : class;

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
    /// <summary>
    /// 清空所有已注册的服务
    /// </summary>
    void Clear();

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

    public void RegisterOrReplace<T>(T instance) where T : class
    {
        if (instance == null) throw new ArgumentNullException(nameof(instance));
        lock (_gate) { _map[typeof(T)] = instance; }
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
            $"Make sure it is registered during bootstrapping."
        );
    }

    public void Clear()
    {
        lock (_gate) _map.Clear();
    }


    public bool IsRegistered<T>() where T : class => TryGet<T>(out _);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// 打印所有已注册的服务
    /// </summary>
    /// <returns></returns>
    public string Dump()
    {
        lock (_gate)
        {
            var names = new List<string>(_map.Count);
            foreach (var k in _map.Keys) names.Add(k.FullName);
            names.Sort(StringComparer.Ordinal);
            return string.Join("\n", names);
        }
    }
#endif
}
