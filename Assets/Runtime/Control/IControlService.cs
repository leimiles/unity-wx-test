using UnityEngine;
using System;

public interface IControlService
{
    /// <summary>
    /// control service 必须有 camera control rig，但是不关心什么事 camera control rig
    /// </summary>
    ICameraControlRig CameraControlRig { get; set; }
    void SwitchCameraControlRig(ICameraControlRig cameraControlRig);
}

public class ControlService : IControlService
{
    public ICameraControlRig CameraControlRig { get; set; }
    public void SwitchCameraControlRig(ICameraControlRig cameraControlRig)
    {
        Debug.Log($"[ControlService] switch camera control rig: {cameraControlRig}");
        CameraControlRig = cameraControlRig ?? throw new ArgumentNullException(nameof(cameraControlRig));
    }
}