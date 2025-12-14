using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MilesUtils;

public class GameManager : PersistentSingleton<GameManager>
{
    EventBinding<BootstrapStartEvent> _bootstrapStartBinding;

    protected override void Awake()
    {
        base.Awake();

        _bootstrapStartBinding = new EventBinding<BootstrapStartEvent>(OnBootstrapStart);
        EventBus<BootstrapStartEvent>.Register(_bootstrapStartBinding);
    }

    void OnBootstrapStart(BootstrapStartEvent e)
    {
        Debug.Log("BootstrapStart");
    }
}
