using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public interface IGameWorld
{
    public string Name { get; }
    public Transform StartPosition { get; }
}

public readonly struct GameWorldEnterEvent : IEvent
{
    public CancellationToken CancellationToken { get; }
    public GameWorldEnterEvent(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
    }
}