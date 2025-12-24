using System.Collections.Generic;
using UnityEngine;

public static class EventBus<T>
    where T : IEvent
{
    static readonly HashSet<IEventBinding<T>> bindings = new HashSet<IEventBinding<T>>();
    static readonly object bindingsLock = new object();

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
        // 在锁内创建快照副本，确保线程安全
        // 虽然每次创建新 List 有 GC 压力，但这是保证线程安全的最简单方式
        List<IEventBinding<T>> snapshot;
        lock (bindingsLock)
        {
            snapshot = new List<IEventBinding<T>>(bindings);
        }

        // 在锁外迭代快照，避免在回调执行期间持有锁
        for (int i = 0; i < snapshot.Count; i++)
        {
            var binding = snapshot[i];

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
        }
    }
}