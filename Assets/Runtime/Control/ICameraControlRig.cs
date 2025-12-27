using UnityEngine;
using System;

public interface ICameraControlRig : IControlRig
{
    Transform CameraRoot { get; }
}

public class JustEntryCameraControlRig : ICameraControlRig
{
    public Transform CameraRoot => _cameraRoot;
    readonly Transform _cameraRoot;
    public bool IsAttached => _isAttached;
    bool _isAttached = false;
    public JustEntryCameraControlRig(ICameraService cameraService)
    {
        Debug.Log($"[JustEntryCameraControlRig] new instance with camera root: {cameraService.CameraRoot}");
        var cameraRoot = cameraService.CameraRoot;
        if (cameraRoot == null)
        {
            throw new ArgumentNullException(nameof(cameraRoot));
        }
        _cameraRoot = cameraRoot;
        _isAttached = true;
    }
}