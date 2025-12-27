using UnityEngine;
using System;

public interface ICameraService
{
    Camera MainCamera { get; }
    bool HasMainCamera { get; }
    Transform CameraRoot { get; }
}

public class CameraService : ICameraService
{
    public Camera MainCamera => _mainCamera;
    public bool HasMainCamera => _mainCamera != null;
    public Transform CameraRoot => _cameraRoot;
    readonly Camera _mainCamera;
    readonly Transform _cameraRoot;
    bool _disposed = false;

    public CameraService(Camera mainCamera)
    {
        _mainCamera = mainCamera != null ? mainCamera : throw new InvalidOperationException("Main camera not found");
        _cameraRoot = new GameObject("[CameraServiceRoot]").transform;
        GameObject.DontDestroyOnLoad(_cameraRoot);
        InitializeHierarchy();
    }

    void InitializeHierarchy()
    {
        ResetCamera(_mainCamera);
        _mainCamera.transform.SetParent(_cameraRoot, false);
    }

    void ResetCamera(Camera camera)
    {
        camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_cameraRoot != null)
        {
            UnityEngine.Object.Destroy(_cameraRoot.gameObject);
        }

        _disposed = true;
    }
}