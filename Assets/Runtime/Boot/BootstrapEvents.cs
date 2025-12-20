
public struct BootstrapCompleteEvent : IEvent
{
    public bool isSuccess;
    public string message;
    public float totalTime;

}

public struct BootstrapProgressEvent : IEvent
{
    public float progress;
    public string message;
}
