using UnityEngine;
using System;

public interface IControlService
{
    void OnWorldReady(IGameWorld world);
}

public class ControlService : IControlService
{
    readonly Transform _cameraRoot;
    ICameraControlRig _currentRig;
    bool _disposed = false;
    public ControlService(Transform cameraRoot)
    {
        _cameraRoot = cameraRoot ?? throw new ArgumentNullException(nameof(cameraRoot));
        _currentRig = new JustEntryCameraControlRig();
    }

    public void OnWorldReady(IGameWorld world)
    {
        if (_disposed)
        {
            Debug.LogWarning("[ControlService] Service already disposed");
            return;
        }

        if (_currentRig == null)
        {
            Debug.LogError("[ControlService] _currentRig is null");
            return;
        }

        if (world == null)
        {
            Debug.LogError("[ControlService] world is null");
            return;
        }

        _currentRig.Detach();
        _currentRig.Attach(_cameraRoot);
        _currentRig.ApplyWorld(world);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _currentRig?.Detach();
        _currentRig = null;
        _disposed = true;
    }
}