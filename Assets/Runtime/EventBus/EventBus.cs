using System.Collections.Generic;
using UnityEngine;

public static class EventBus<T>
    where T : IEvent
{
    static readonly HashSet<IEventBinding<T>> bindings = new HashSet<IEventBinding<T>>();
    static readonly object bindingsLock = new object();

    // 复用 List 避免每次分配，减少 GC 压力
    static readonly List<IEventBinding<T>> snapshotList = new List<IEventBinding<T>>();

    public static void Register(EventBinding<T> binding)
    {
        lock (bindingsLock)
        {
            bindings.Add(binding);
        }
    }

    public static void Deregister(EventBinding<T> binding)
    {
        lock (bindingsLock)
        {
            bindings.Remove(binding);
        }
    }

    public static void Raise(T @event)
    {
        // 在锁内快速复制到 List（复用，避免 GC 分配）
        lock (bindingsLock)
        {
            snapshotList.Clear();
            snapshotList.AddRange(bindings);
        }

        // 在锁外迭代，避免在回调执行期间持有锁
        // 注意：允许在回调执行期间移除 binding，这是可接受的行为
        for (int i = 0; i < snapshotList.Count; i++)
        {
            var binding = snapshotList[i];

            // 可选：检查 binding 是否仍然在集合中（如果需要在回调期间移除时跳过）
            // 但为了性能，可以省略这个检查，允许回调执行即使 binding 已被移除
            // 这在大多数情况下是可接受的，因为回调通常很快完成

            try
            {
                binding.OnEvent?.Invoke(@event);
                binding.OnEventNoArgs?.Invoke();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EventBus] Exception in event handler for {typeof(T).Name}: {ex.Message}");
            }
        }
    }

    public static void Clear()
    {
        lock (bindingsLock)
        {
            bindings.Clear();
            snapshotList.Clear();
        }
    }
}