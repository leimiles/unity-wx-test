using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SubSystemInitializationStartEvent : IEvent
{
    public string subSystemName;
    public int priority;
}

public struct SubSystemInitializationProgressEvent : IEvent
{
    public float progress;
    public float totalProgress;
}

public struct SubSystemInitializationCompleteEvent : IEvent
{
    public string subSystemName;
    public bool isSuccess;
    public string message;
}