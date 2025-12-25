using System;

public interface IEventBinding<T>
{
    public Action<T> OnEvent { get; set; }
    public Action OnEventNoArgs { get; set; }
}

public class EventBinding<T> : IEventBinding<T>, IDisposable
    where T : IEvent
{
    bool _disposed = false;
    Action<T> onEvent = _ => { };
    Action onEventNoArgs = () => { };

    Action<T> IEventBinding<T>.OnEvent
    {
        get => onEvent;
        set => onEvent = value;
    }

    Action IEventBinding<T>.OnEventNoArgs
    {
        get => onEventNoArgs;
        set => onEventNoArgs = value;
    }

    public EventBinding(Action<T> onEvent) => this.onEvent = onEvent;

    public EventBinding(Action onEventNoArgs) => this.onEventNoArgs = onEventNoArgs;

    public void Add(Action onEvent) => onEventNoArgs += onEvent;

    public void Remove(Action onEvent) => onEventNoArgs -= onEvent;

    public void Add(Action<T> onEvent) => this.onEvent += onEvent;

    public void Remove(Action<T> onEvent) => this.onEvent -= onEvent;

    public void Dispose()
    {
        if (!_disposed)
        {
            // 先注销，再清理委托引用
            EventBus<T>.Deregister(this);

            // 清理委托引用
            onEvent = null;
            onEventNoArgs = null;

            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(EventBinding<T>));
        }
    }
}
