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
    Transform _cameraRoot;
    public void Attach(Transform cameraRoot)
    {
        _cameraRoot = cameraRoot;
        Debug.Log($"[JustEntryCameraControlRig] attach with camera root: {cameraRoot.name}");
    }

    public void Detach()
    {
        Debug.Log($"[JustEntryCameraControlRig] detach");
        _cameraRoot = null;
    }


    public void ApplyWorld(IGameWorld world)
    {
        if (world == null)
        {
            Debug.LogError("[JustEntryCameraControlRig] world is null");
            return;
        }

        if (world.StartPosition == null)
        {
            Debug.LogError("[JustEntryCameraControlRig] world.StartPosition is null");
            return;
        }

        if (_cameraRoot == null)
        {
            Debug.LogError("[JustEntryCameraControlRig] cameraRoot is null");
            return;
        }

        Debug.Log($"[JustEntryCameraControlRig] apply world: {world.Name}");
        _cameraRoot.transform.SetPositionAndRotation(world.StartPosition.position, world.StartPosition.rotation);
    }
}