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
            bootstrapConfigs.Validate();
            EventBus<BootstrapStartEvent>.Raise(
                new BootstrapStartEvent { bootstrapConfigs = bootstrapConfigs }
            );

            // 暂时不需要 GameManager 的实例，因为 GameManager 是 PersistentSingleton，会自动在启动时创建实例
            //var gameManager = GameManager.Instance;
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
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
