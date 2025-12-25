using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface ICameraService
{
    Camera MainCamera { get; }
    bool HasMainCamera { get; }
}

public class CameraService : ICameraService
{
    public Camera MainCamera => _mainCamera;
    Camera _mainCamera;
    public bool HasMainCamera => _mainCamera != null;
    public CameraService()
    {
        InitializeMainCamera();
    }

    void InitializeMainCamera()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            throw new InvalidOperationException("Main camera not found");
        }
    }
}