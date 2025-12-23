using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public interface IFlowFactory
{
    IGameFlow CreateFlow(FlowID flowID);
}

public sealed class FlowFactory : IFlowFactory
{
    readonly IGameServices _services;
    public FlowFactory(IGameServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }
    public IGameFlow CreateFlow(FlowID id)
    {
        return id switch
        {
            FlowID.TestScene => new TestSceneFlow(_services),
            FlowID.TestUI => new TestUIFlow(_services),
            _ => throw new System.ArgumentOutOfRangeException(nameof(id), id, "Unknown flow id")
        };
    }
}
