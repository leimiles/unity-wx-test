
public struct SubSystemInitializationStartEvent : IEvent
{
    public string subSystemName;
    public int priority;
}

public struct SubSystemInitializationProgressEvent : IEvent
{
    public string subSystemName;
    public float progress;
    public float totalProgress;
}

public struct SubSystemInitializationCompleteEvent : IEvent
{
    public string subSystemName;
    public bool isSuccess;
    public string message;
}