using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;

[DisallowMultipleComponent]
public class BootUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] Slider _progressSlider;
    [SerializeField] TextMeshProUGUI _progressText;
    [SerializeField] Image _LoadingBackgroundImage;

    [Header("Fade")]
    [SerializeField] CanvasGroup _canvasGroup;
    [SerializeField] float _fadeInSeconds = 0.25f;
    [SerializeField] float _fadeOutSeconds = 0.25f;

    EventBinding<BootstrapProgressEvent> _progressBinding;
    EventBinding<BootstrapCompleteEvent> _completeBinding;
    EventBinding<SubSystemInitializationStartEvent> _subSystemStartBinding;
    EventBinding<SubSystemInitializationProgressEvent> _subSystemProgressBinding;
    EventBinding<SubSystemInitializationCompleteEvent> _subSystemCompleteBinding;
    CancellationTokenSource _fadeCts;

    void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_progressSlider != null)
        {
            _progressSlider.minValue = 0f;
            _progressSlider.maxValue = 1f;
        }
    }

    void OnEnable()
    {
        _progressBinding = new EventBinding<BootstrapProgressEvent>(OnBootProgress);
        EventBus<BootstrapProgressEvent>.Register(_progressBinding);

        _completeBinding = new EventBinding<BootstrapCompleteEvent>(OnBootComplete);
        EventBus<BootstrapCompleteEvent>.Register(_completeBinding);

        _subSystemStartBinding = new EventBinding<SubSystemInitializationStartEvent>(OnSubSystemStart);
        EventBus<SubSystemInitializationStartEvent>.Register(_subSystemStartBinding);

        _subSystemProgressBinding = new EventBinding<SubSystemInitializationProgressEvent>(OnSubSystemProgress);
        EventBus<SubSystemInitializationProgressEvent>.Register(_subSystemProgressBinding);

        _subSystemCompleteBinding = new EventBinding<SubSystemInitializationCompleteEvent>(OnSubSystemComplete);
        EventBus<SubSystemInitializationCompleteEvent>.Register(_subSystemCompleteBinding);

        // 启动淡入（不阻塞）
        _ = FadeInAsync();
    }

    void OnDisable()
    {
        if (_progressBinding != null) EventBus<BootstrapProgressEvent>.Deregister(_progressBinding);
        _progressBinding = null;

        if (_completeBinding != null) EventBus<BootstrapCompleteEvent>.Deregister(_completeBinding);
        _completeBinding = null;

        CancelFade();
    }

    void OnBootProgress(BootstrapProgressEvent e)
    {
        var p01 = Mathf.Clamp01(e.progress);

        if (_progressSlider != null) _progressSlider.value = p01;
        if (_progressText != null) _progressText.text = $"{p01 * 100f:0}%";
    }

    void OnBootComplete(BootstrapCompleteEvent e)
    {
        if (!e.isSuccess) return;

        // 最小收口：淡出后隐藏（不 Destroy）
        _ = FadeOutThenHideAsync();
    }

    void OnSubSystemStart(SubSystemInitializationStartEvent e)
    {
        if (_progressText != null) _progressText.text = $"({e.subSystemName} | Starting...) - {_progressText.text}";
    }

    void OnSubSystemProgress(SubSystemInitializationProgressEvent e)
    {
        var p01 = Mathf.Clamp01(e.progress);
        if (_progressText != null) _progressText.text = $"({e.subSystemName} | {p01 * 100f:0}%) - {_progressText.text}";
    }

    void OnSubSystemComplete(SubSystemInitializationCompleteEvent e)
    {
        if (_progressText != null) _progressText.text = $"({e.subSystemName} | {e.message}) - {_progressText.text}";
    }

    async UniTask FadeInAsync()
    {
        if (_canvasGroup == null) return;

        CancelFade();
        _fadeCts = new CancellationTokenSource();

        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = true;

        await FadeToAsync(1f, _fadeInSeconds, _fadeCts.Token);

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = true;
    }

    async UniTask FadeOutThenHideAsync()
    {
        if (_canvasGroup == null)
        {
            gameObject.SetActive(false);
            return;
        }

        CancelFade();
        _fadeCts = new CancellationTokenSource();

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = true;

        await FadeToAsync(0f, _fadeOutSeconds, _fadeCts.Token);

        // 触发 OnDisable -> 自动解绑事件
        gameObject.SetActive(false);
    }

    async UniTask FadeToAsync(float targetAlpha, float duration, CancellationToken ct)
    {
        if (duration <= 0f)
        {
            _canvasGroup.alpha = targetAlpha;
            return;
        }

        float start = _canvasGroup.alpha;
        float t = 0f;

        while (t < duration)
        {
            ct.ThrowIfCancellationRequested();
            t += Time.unscaledDeltaTime; // 启动期用 unscaled 更稳
            float k = Mathf.Clamp01(t / duration);
            _canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, k);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        _canvasGroup.alpha = targetAlpha;
    }

    void CancelFade()
    {
        if (_fadeCts == null) return;
        _fadeCts.Cancel();
        _fadeCts.Dispose();
        _fadeCts = null;
    }
}
