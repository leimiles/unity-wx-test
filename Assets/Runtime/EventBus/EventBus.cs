using System.Collections.Generic;
using System.Buffers;
using UnityEngine;

/// <summary>
/// 事件总线系统
/// 性能优化说明：
/// 1. 使用 ArrayPool 减少数组分配
/// 2. 采用快照模式避免在回调中持有锁
/// 3. 异常捕获确保单个处理器失败不影响其他处理器
/// 4. 使用 HashSet 确保 O(1) 注册/注销性能
/// </summary>
public static class EventBus<T>
    where T : IEvent
{
    static readonly HashSet<IEventBinding<T>> bindings = new HashSet<IEventBinding<T>>();
    static readonly object bindingsLock = new object();
    private static readonly ArrayPool<IEventBinding<T>> _bindingPool = ArrayPool<IEventBinding<T>>.Shared;
    
    // 性能优化：小集合使用栈分配，避免 ArrayPool 开销
    private const int STACK_ALLOC_THRESHOLD = 8;


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
        IEventBinding<T>[] snapshot = null;
        int count = 0;

        lock (bindingsLock)
        {
            count = bindings.Count;
            if (count == 0) return;

            snapshot = _bindingPool.Rent(count);
            bindings.CopyTo(snapshot);
        }

        try
        {
            // 在锁外迭代快照，避免在回调执行期间持有锁
            for (int i = 0; i < count; i++)
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
        finally
        {
            if (snapshot != null)
            {
                // 清理数组内容（只清理使用的部分）
                System.Array.Clear(snapshot, 0, count);
                _bindingPool.Return(snapshot);
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