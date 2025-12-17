using UnityEngine;

[DisallowMultipleComponent]
public class Bootstrap : MonoBehaviour
{
    [SerializeField] int frameRate = 60;
    [SerializeField] bool runInBackground = true;
    [SerializeField] BootstrapConfigs bootstrapConfigs;

    EventBinding<BootstrapCompleteEvent> _bootCompleteBinding;

    void Awake()
    {
        Application.targetFrameRate = frameRate;
        Application.runInBackground = runInBackground;

        _bootCompleteBinding = new EventBinding<BootstrapCompleteEvent>(OnBootComplete);
        EventBus<BootstrapCompleteEvent>.Register(_bootCompleteBinding);
    }

    void Start()
    {
        try
        {
            UISubSystem.ShowLoadingUI();
            if (bootstrapConfigs == null)
            {
                Debug.LogError("BootstrapConfigs is null, cannot start bootstrap");
                EventBus<BootstrapCompleteEvent>.Raise(
                    new BootstrapCompleteEvent
                    {
                        isSuccess = false,
                        message = "BootstrapConfigs is null",
                        totalTime = 0f
                    }
                );
                return;
            }

            bootstrapConfigs.Validate();
            var gameManager = GameManager.Instance; // 确保 GameManager 已经初始化
            EventBus<BootstrapStartEvent>.Raise(
                new BootstrapStartEvent { bootstrapConfigs = bootstrapConfigs }
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Bootstrap Start failed: {e}");
            EventBus<BootstrapCompleteEvent>.Raise(
                new BootstrapCompleteEvent
                {
                    isSuccess = false,
                    message = e.Message,
                    totalTime = 0f
                }
            );
        }
    }

    void OnDestroy()
    {
        if (_bootCompleteBinding != null)
        {
            EventBus<BootstrapCompleteEvent>.Deregister(_bootCompleteBinding);
        }
    }

    void OnBootComplete(BootstrapCompleteEvent e)
    {
        if (e.isSuccess)
        {
            Debug.Log("Bootstrap complete");
            //TODO: 加载主场景
        }
        else
        {
            Debug.LogError("Bootstrap failed: " + e.message);
        }
    }

}
