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

    // 单击
    public System.Action<Vector2> onTapEvent;

    // 双击
    public System.Action<Vector2> onDoubleTapEvent;
    [SerializeField] float doubleTapInterval = 0.3f;
    [SerializeField] float doubleTapMoveDistance = 50f;
    float lastTapTime = -1f;
    Vector2 lastTapPosition;

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
        var currentTapPosition = context.ReadValue<Vector2>();
        //Debug.Log($"OnTapPerformed: {touchPosition}");
        float currentTime = Time.time;

        if (currentTime - lastTapTime < doubleTapInterval && Vector2.Distance(currentTapPosition, lastTapPosition) < doubleTapMoveDistance)
        {
            Debug.Log("Double Tap");
            onDoubleTapEvent?.Invoke(currentTapPosition);
            lastTapTime = -1f;
        }
        else
        {
            Debug.Log("Single Tap");
            onTapEvent?.Invoke(currentTapPosition);
            lastTapTime = currentTime;
            lastTapPosition = currentTapPosition;
        }

    }


}
