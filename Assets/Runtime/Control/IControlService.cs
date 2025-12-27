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
    public ControlService(Transform cameraRoot)
    {
        _cameraRoot = cameraRoot ?? throw new ArgumentNullException(nameof(cameraRoot));
        _currentRig = new JustEntryCameraControlRig();
    }

    public void OnWorldReady(IGameWorld world)
    {
        _currentRig.Detach();
        _currentRig.Attach(_cameraRoot);
        _currentRig.ApplyWorld(world);
    }
}