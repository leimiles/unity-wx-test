using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DemoWorld : MonoBehaviour, IGameWorld
{
    [SerializeField] private Transform _startPosition;
    public string Name => "DemoWorld";

    // 如果设置过 StartPosition，则使用设置的 StartPosition，否则使用 GameObject 的 Transform
    public Transform StartPosition => _startPosition != null ? _startPosition : transform;

}
