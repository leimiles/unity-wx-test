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
        HashSet<IEventBinding<T>> snapshot;
        lock (bindingsLock)
        {
            snapshot = new HashSet<IEventBinding<T>>(bindings);
        }

        foreach (var binding in snapshot)
        {
            lock (bindingsLock)
            {
                if (bindings.Contains(binding))
                {
                    binding.OnEvent.Invoke(@event);
                    binding.OnEventNoArgs.Invoke();
                }
            }
        }
    }

    public static void Clear()
    {
        lock (bindingsLock)
        {
            //Debug.Log($"Clearing {typeof(T).Name} bindings");
            bindings.Clear();
        }
    }
}
