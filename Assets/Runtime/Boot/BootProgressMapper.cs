using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public sealed class BootProgressMapper
{
    private readonly IProgress<float> _boot;
    private readonly string _name;
    private readonly float _base;
    private readonly float _weight;

    public BootProgressMapper(IProgress<float> boot, string name, int processed, int total)
    {
        _boot = boot;
        _name = name;
        _base = processed / (float)total;
        _weight = 1.0f / total;
    }

    public IProgress<float> Create()
    {
        return new Progress<float>(p =>
        {
            p = Mathf.Clamp01(p);
            float totalProgress = Mathf.Clamp01(_base + p * _weight);
            _boot?.Report(totalProgress);

            EventBus<SubSystemInitializationProgressEvent>.Raise(
                new SubSystemInitializationProgressEvent
                {
                    subSystemName = _name,
                    progress = p,
                    totalProgress = totalProgress
                });
        });
    }
}
