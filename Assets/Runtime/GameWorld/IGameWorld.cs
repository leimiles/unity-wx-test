using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public interface IGameWorld
{
    public string Name { get; }
    public Transform StartPosition { get; }
}

public struct GameWorldEnterEvent : IEvent
{
    public string gameWorldName;
}