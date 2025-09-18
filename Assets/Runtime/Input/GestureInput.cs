using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

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

    // 长按
    public System.Action<Vector2> onHoldingEvent;
    public System.Action<Vector2> onHoldingEndEvent;
    bool isHolding = false;

    // 滑动
    public System.Action<Vector2> onSlideEvent;

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
        input.Gesture.Hold.performed += OnHoldPerformed;
        input.Gesture.Hold.canceled += OnHoldCanceled;
        input.Gesture.Slide.performed += OnSlidePerformed;
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        input.Gesture.Disable();
        input.Gesture.Tap.performed -= OnTapPerformed;
        input.Gesture.Hold.performed -= OnHoldPerformed;
        input.Gesture.Hold.canceled -= OnHoldCanceled;
        input.Gesture.Slide.performed -= OnSlidePerformed;
    }

    // 滑动时触发
    void OnSlidePerformed(InputAction.CallbackContext context)
    {
        if (Touch.activeTouches.Count != 1) return;
        // 滑动是一种特殊的 hold 操作，所以会将 isHolding 设置为 true
        isHolding = true;
        Debug.Log("Slide Performed");
        onSlideEvent?.Invoke(Touch.activeTouches[0].screenPosition);
    }

    // 根据条件触发单击或者双击，双击触发包含单击触发
    void OnTapPerformed(InputAction.CallbackContext context)
    {
        var currentTapPosition = context.ReadValue<Vector2>();
        float currentTime = Time.time;

        if (currentTime - lastTapTime < doubleTapInterval && Vector2.Distance(currentTapPosition, lastTapPosition) < doubleTapMoveDistance)
        {
            Debug.Log("Double Tap");
            lastTapTime = -1f;
            onDoubleTapEvent?.Invoke(currentTapPosition);
        }
        else
        {
            Debug.Log("Single Tap");
            lastTapTime = currentTime;
            lastTapPosition = currentTapPosition;
            onTapEvent?.Invoke(currentTapPosition);
        }

    }

    // 长按开始时触发
    void OnHoldPerformed(InputAction.CallbackContext context)
    {
        if (Touch.activeTouches.Count != 1) return;
        Debug.Log("Hold Performed");
        isHolding = true;
    }

    // 长按结束时触发
    void OnHoldCanceled(InputAction.CallbackContext context)
    {
        if (Touch.activeTouches.Count != 1 || !isHolding) return;
        Debug.Log("Hold Canceled");
        isHolding = false;
        onHoldingEndEvent?.Invoke(Touch.activeTouches[0].screenPosition);
    }

    // 长按过程中触发，通过 update 表现出来
    void OnHoldingCallback(bool isHolding)
    {
        if (Touch.activeTouches.Count != 1) return;
        if (isHolding)
        {
            Debug.Log("Holding");
            var pos = Touch.activeTouches[0].screenPosition;
            onHoldingEvent?.Invoke(pos);
        }
    }

    void Update()
    {
        OnHoldingCallback(isHolding);
    }




}
