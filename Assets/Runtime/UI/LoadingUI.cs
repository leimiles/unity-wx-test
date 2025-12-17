using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] Slider _progressSlider;
    [SerializeField] TextMeshProUGUI _progressText;
    [SerializeField] Image _LoadingBackgroundImage;

    EventBinding<BootstrapProgressEvent> _progressBinding;
    EventBinding<BootstrapCompleteEvent> _completeBinding;

    void OnEnable()
    {
        _progressBinding = new EventBinding<BootstrapProgressEvent>(OnBootProgress);
        EventBus<BootstrapProgressEvent>.Register(_progressBinding);

        _completeBinding = new EventBinding<BootstrapCompleteEvent>(OnBootComplete);
        EventBus<BootstrapCompleteEvent>.Register(_completeBinding);
    }

    void OnDisable()
    {
        if (_progressBinding != null) EventBus<BootstrapProgressEvent>.Deregister(_progressBinding);
        _progressBinding = null;
        if (_completeBinding != null) EventBus<BootstrapCompleteEvent>.Deregister(_completeBinding);
        _completeBinding = null;
    }

    void OnBootProgress(BootstrapProgressEvent e)
    {
        if (_progressSlider == null || _progressText == null) return;
        var p01 = Mathf.Clamp01(e.progress);
        _progressSlider.value = p01;              // slider maxValue = 1
        _progressText.text = $"{p01 * 100f:0}%";  // 文本显示百分比

    }

    void OnBootComplete(BootstrapCompleteEvent e)
    {
        if (!e.isSuccess) return;

        // 最小收口：隐藏（不 Destroy），Destroy 交给外部更稳
        //gameObject.SetActive(false);
    }
}
