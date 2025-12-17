using UnityEngine;

internal class LoadingUIService
{
    static GameObject _loadingUIPrefab;
    static GameObject _loadingUIInstance;

    static LoadingUIService _instance;
    public static LoadingUIService Instance => _instance ??= new LoadingUIService();

    bool _eventsRegistered;
    EventBinding<BootstrapProgressEvent> _progressBinding;
    EventBinding<BootstrapCompleteEvent> _completeBinding;

    public void Show()
    {
        RegisterEventsOnce();

        if (_loadingUIInstance != null)
            return;

        _loadingUIPrefab ??= Resources.Load<GameObject>("UI/Canvas_Loading");

        if (_loadingUIPrefab == null)
        {
            Debug.LogError("Loading UI prefab not found");
            return;
        }

        _loadingUIInstance = GameObject.Instantiate(_loadingUIPrefab);

        GameObject.DontDestroyOnLoad(_loadingUIInstance);
    }

    void RegisterEventsOnce()
    {
        if (_eventsRegistered) return;
        _eventsRegistered = true;

        _progressBinding = new EventBinding<BootstrapProgressEvent>(OnBootProgress);
        EventBus<BootstrapProgressEvent>.Register(_progressBinding);

        _completeBinding = new EventBinding<BootstrapCompleteEvent>(OnBootComplete);
        EventBus<BootstrapCompleteEvent>.Register(_completeBinding);
    }

    void OnBootProgress(BootstrapProgressEvent e)
    {
        _loadingUIInstance.GetComponent<LoadingUI>().SetProgress(e.progress);
    }

    void OnBootComplete(BootstrapCompleteEvent e)
    {
        if (!e.isSuccess) return;
        if (_loadingUIInstance == null) return;

        //_loadingUIInstance.SetActive(false);

        // 可选：你如果希望彻底收口（避免泄漏），可以在成功后解绑：
        // EventBus<BootstrapProgressEvent>.Deregister(_progressBinding);
        // EventBus<BootstrapCompleteEvent>.Deregister(_completeBinding);
        // _eventsRegistered = false;
    }
}
