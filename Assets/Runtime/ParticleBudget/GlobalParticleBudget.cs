using UnityEngine;
using System.Collections.Generic;

public class GlobalParticleBudget : MonoBehaviour
{
    public static GlobalParticleBudget I;

    [Header("Global Budget")]
    public int maxParticles = 8000;          // 全局上限（估算值）
    public bool softScaleBeforeReject = true;// 先软缩放发射率
    [Range(0.1f, 1f)] public float minGlobalScale = 0.3f;

    private int _currentEstimated;           // 已占用估计
    private readonly List<BudgetedParticles> _actives = new List<BudgetedParticles>(128);
    private float _globalScale = 1f;

    void Awake() { I = this; }

    // 注册/反注册在播系统（由 BudgetedParticles 调用）
    public void Register(BudgetedParticles b)
    {
        // 避免重复
        for (int i = 0; i < _actives.Count; i++) if (_actives[i] == b) return;
        _actives.Add(b);
        _currentEstimated += b.CurrentCost();
        Rebalance();
    }
    public void Unregister(BudgetedParticles b)
    {
        for (int i = 0; i < _actives.Count; i++)
        {
            if (_actives[i] == b)
            {
                _actives.RemoveAt(i);
                _currentEstimated = Mathf.Max(0, _currentEstimated - b.CurrentCost());
                Rebalance();
                return;
            }
        }
    }

    // 申请播放前询问是否允许；可能会降级
    public bool TryAcquire(BudgetedParticles req)
    {
        // 先尝试当前 LOD
        if (_currentEstimated + req.CostForLOD(req.lod) <= maxParticles)
        {
            _currentEstimated += req.CostForLOD(req.lod);
            return true;
        }

        // 尝试更低 LOD
        int bestLod = req.lod;
        for (int l = req.lod + 1; l < req.lodCount; l++)
        {
            int cost = req.CostForLOD(l);
            if (_currentEstimated + cost <= maxParticles)
            {
                req.ApplyLOD(l);
                _currentEstimated += cost;
                return true;
            }
        }

        // 软缩放：把所有在播系统的 emission 按比例乘一个系数
        if (softScaleBeforeReject)
        {
            float need = (float)(_currentEstimated + req.CostForLOD(req.lod));
            float scale = Mathf.Clamp(maxParticles / Mathf.Max(need, 1f), minGlobalScale, 1f);
            if (scale < 1f)
            {
                _globalScale = scale;
                ApplyGlobalScale();
                // 重算后再试一次
                if (_currentEstimated + req.CostForLOD(req.lod) <= maxParticles)
                {
                    _currentEstimated += req.CostForLOD(req.lod);
                    return true;
                }
            }
        }

        // 按优先级淘汰：暂停一些低优先级在播系统，空出预算
        int freed = 0;
        for (int pr = (int)VfxPriority.Low; pr >= (int)VfxPriority.Critical; pr--)
        {
            for (int i = _actives.Count - 1; i >= 0; i--)
            {
                var a = _actives[i];
                if ((int)a.priority == pr && a.canBePausedWhenStarved)
                {
                    freed += a.CurrentCost();
                    a.PauseAndHide();
                    _actives.RemoveAt(i);
                    if (_currentEstimated >= freed) _currentEstimated -= freed;
                    if (_currentEstimated + req.CostForLOD(req.lod) <= maxParticles)
                    {
                        _currentEstimated += req.CostForLOD(req.lod);
                        return true;
                    }
                }
            }
        }

        // 实在不行，拒绝
        return false;
    }

    // 重新平衡：超限时下调，全空时恢复
    private void Rebalance()
    {
        if (_currentEstimated <= maxParticles)
        {
            if (_globalScale != 1f)
            {
                _globalScale = Mathf.Min(1f, _globalScale + Time.unscaledDeltaTime * 0.5f);
                ApplyGlobalScale();
            }
            return;
        }
        float scale = Mathf.Clamp((float)maxParticles / Mathf.Max(_currentEstimated, 1f), minGlobalScale, 1f);
        if (scale < _globalScale)
        {
            _globalScale = scale;
            ApplyGlobalScale();
        }
    }

    private void ApplyGlobalScale()
    {
        for (int i = 0; i < _actives.Count; i++)
        {
            _actives[i].ApplyGlobalScale(_globalScale);
        }
    }
}

public enum VfxPriority { Critical = 0, High = 1, Normal = 2, Low = 3 }
