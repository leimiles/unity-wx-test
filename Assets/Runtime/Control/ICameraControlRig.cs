using UnityEngine;
using System;

public interface ICameraControlRig
{
    void Attach(Transform cameraRoot);
    void Detach();
    void ApplyWorld(IGameWorld world);
}

public class JustEntryCameraControlRig : ICameraControlRig
{
    Transform cameraRoot;
    public void Attach(Transform cameraRoot)
    {
        this.cameraRoot = cameraRoot;
        Debug.Log($"[JustEntryCameraControlRig] attach with camera root: {cameraRoot.name}");
    }

    public void Detach()
    {
        Debug.Log($"[JustEntryCameraControlRig] detach");
        cameraRoot = null;
    }

    public void ApplyWorld(IGameWorld world)
    {
        Debug.Log($"[JustEntryCameraControlRig] apply world: {world.Name}");
        cameraRoot.transform.SetPositionAndRotation(world.StartPosition.position, world.StartPosition.rotation);
    }
}