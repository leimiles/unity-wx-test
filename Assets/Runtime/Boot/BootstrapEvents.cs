using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct BootstrapCompleteEvent : IEvent
{
    public bool isSuccess;
    public string message;
    public float totalTime;

}

public struct BootstrapStartEvent : IEvent
{
    public BootstrapConfigs bootstrapConfigs;
}
