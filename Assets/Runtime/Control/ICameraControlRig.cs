using System.Collections;
using System.Collections.Generic;
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
    public JustEntryCameraControlRig(Transform cameraRoot)
    {
        _cameraRoot = cameraRoot ?? throw new ArgumentNullException(nameof(cameraRoot));
    }
    public void Attach()
    {
        _isAttached = true;
    }
    public void Detach()
    {
        _isAttached = false;
    }
    public void Reset()
    {
        _isAttached = false;
    }
}