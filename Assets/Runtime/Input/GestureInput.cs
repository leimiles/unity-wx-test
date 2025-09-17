using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using EnhancedTouchPhase = UnityEngine.InputSystem.TouchPhase;

[DisallowMultipleComponent]
public class GestureInput : MonoBehaviour
{
    float maxTapTime = 0.3f;
    float maxTapMoveDistance = 10f;
    bool isTouching = false;
    float touchStartTime;
    Vector2 touchStartPosition;

    System.Action<Vector2> onTap;

    IA_Game input;

    void Awake()
    {
        input = new IA_Game();
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        input.Gesture.Enable();
        input.Gesture.Tap.performed += OnTapPerformed;
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        input.Gesture.Disable();
        input.Gesture.Tap.performed -= OnTapPerformed;
    }

    void OnTapPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("OnTapPerformed");

        var touchPosition = context.ReadValue<Vector2>();
        Debug.Log($"OnTapPerformed: {touchPosition}");

    }


}
