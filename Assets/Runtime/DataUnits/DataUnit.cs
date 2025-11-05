using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DataUnit<T> : MonoBehaviour, IDisposable
{
    public void Dispose()
    {
        // Clean up resources
        Debug.Log($"Disposing {typeof(T).Name}");
    }

    void OnDestroy()
    {
        Debug.Log($"OnDestroy {typeof(T).Name}");
        Dispose();
    }
}
