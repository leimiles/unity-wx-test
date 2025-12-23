using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public readonly struct RequestFlowSwitchEvent : IEvent
{
    public readonly FlowID Next;
    public RequestFlowSwitchEvent(FlowID next) => Next = next;
}
