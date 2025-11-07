public interface IEvent { }

public struct TestEvent : IEvent { }

public struct PlayerEvent : IEvent
{
    public int health;
    public int mana;
}


public struct SceneLoadingEvent : IEvent
{
    public float progress;
    public string message;
}

public struct SceneLoadingCompleteEvent : IEvent
{
    public bool isSuccess;
    public string message;
}