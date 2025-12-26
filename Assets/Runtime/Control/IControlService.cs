using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IControlService
{
    ICameraControlRig CameraControlRig { get; }
    void SwitchCameraControlRig(ICameraControlRig cameraControlRig);
}

public class ControlService : IControlService
{
    public ICameraControlRig CameraControlRig => _cameraControlRig;
    readonly ICameraService _cameraService;
    ICameraControlRig _cameraControlRig;
    public ControlService(ICameraService cameraService)
    {
        _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
    }

    public void SwitchCameraControlRig(ICameraControlRig cameraControlRig)
    {
        _cameraControlRig = cameraControlRig ?? throw new ArgumentNullException(nameof(cameraControlRig));
    }
}