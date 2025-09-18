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
    bool isHolding = false;

    // 滑动
    public System.Action<Vector2> onSlideEvent;

    IA_Game input;

    void Awake()
    {
        input = new IA_Game();
    }

    // void Start()
    // {
    //     onHoldingEvent += Fun;
    // }

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

    void OnSlideCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("Slide Canceled");
    }

    void OnSlidePerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Slide Performed");
        var currentSlidePosition = context.ReadValue<Vector2>();
        onSlideEvent?.Invoke(currentSlidePosition);
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

    void OnHoldPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Hold Performed");
        isHolding = true;
    }

    void OnHoldCanceled(InputAction.CallbackContext context)
    {
        Debug.Log("Hold Canceled");
        isHolding = false;
    }

    void OnHoldingCallback(bool isHolding)
    {
        if (isHolding)
        {
            Debug.Log("Holding");
            var pos = Touch.activeTouches[0].screenPosition;
            onHoldingEvent?.Invoke(pos);
        }
    }

    // void Fun(Vector2 pos)
    // {
    //     Debug.Log("Fun" + pos);
    // }

    void Update()
    {
        OnHoldingCallback(isHolding);
    }




}
