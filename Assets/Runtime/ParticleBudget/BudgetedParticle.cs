using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 明确自己要消耗多少预算
/// 支持 LOD
/// 响应全局预算管理器
/// 生命周期结束时的自动回收
/// </summary>
[DisallowMultipleComponent]
public class BudgetedParticle : MonoBehaviour
{
    // 估算单次在播成本（LOD0）。cost ≈ rateOverTime * lifetime + Σ(burstCounts）
    [SerializeField] int estimatedCostLOD0 = 600;
    // 各 LOD 的发射率倍率，如 [1, 0.6, 0.35, 0.2]
    [SerializeField] float[] rateMultipliers = new float[] { 1f, 0.6f, 0.35f };

    // 各 LOD 的寿命倍率，如 [1, 0.8, 0.6, 0.5]
    [SerializeField] float[] lifeMultipliers = new float[] { 1f, 0.8f, 0.6f };

    ParticleSystem[] particleSystems;
    float[] baseRates;
    float[] baseLifetimes;
    ParticleSystem.EmissionModule[] emissionModules;
    ParticleSystem.MainModule[] mainModules;
    ParticleSystem.TrailModule[] trailModules;
    bool[] hasTrails;

    void OnValidate()
    {
        Init();
    }

    void Init()
    {
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);

        baseRates = new float[particleSystems.Length];
        baseLifetimes = new float[particleSystems.Length];
        emissionModules = new ParticleSystem.EmissionModule[particleSystems.Length];
        mainModules = new ParticleSystem.MainModule[particleSystems.Length];
        trailModules = new ParticleSystem.TrailModule[particleSystems.Length];
        hasTrails = new bool[particleSystems.Length];

        for (int i = 0; i < particleSystems.Length; i++)
        {
            baseRates[i] = GetRepresentativeValue(particleSystems[i].emission.rateOverTime);
            baseLifetimes[i] = GetRepresentativeValue(particleSystems[i].main.startLifetime);
            emissionModules[i] = particleSystems[i].emission;
            mainModules[i] = particleSystems[i].main;
            trailModules[i] = particleSystems[i].trails;
            hasTrails[i] = trailModules[i].enabled;
        }

        estimatedCostLOD0 = CalculateBudgetCostForLOD(0);   // 估算单次在播成本（LOD0）

    }

    // 根据 LOD 级别应用配置
    public void ApplyLOD(int level)
    {

    }


    enum CurveEvalStrategy { Average, Max, End }

    float GetRepresentativeValue(ParticleSystem.MinMaxCurve curve, CurveEvalStrategy strategy = CurveEvalStrategy.Average)
    {
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return curve.constant;

            case ParticleSystemCurveMode.TwoConstants:
                return (curve.constantMin + curve.constantMax) * 0.5f;

            case ParticleSystemCurveMode.Curve:
                return EvaluateCurve(curve.curve, strategy);

            case ParticleSystemCurveMode.TwoCurves:
                float v1 = EvaluateCurve(curve.curveMin, strategy);
                float v2 = EvaluateCurve(curve.curveMax, strategy);
                return (v1 + v2) * 0.5f;

            default:
                return 0f;
        }
    }

    float EvaluateCurve(AnimationCurve c, CurveEvalStrategy strategy)
    {
        switch (strategy)
        {
            case CurveEvalStrategy.Max:
                float max = float.MinValue;
                foreach (var k in c.keys) max = Mathf.Max(max, k.value);
                return max;
            case CurveEvalStrategy.End:
                return c.Evaluate(c.keys[c.length - 1].time);
            case CurveEvalStrategy.Average:
            default:
                float sum = 0f;
                int samples = 8;    // 8个采样点，默认做8次采样求平均
                float length = c.keys[c.length - 1].time;
                for (int i = 0; i < samples; i++)
                {
                    float t = length * i / (samples - 1);
                    sum += c.Evaluate(t);
                }
                return sum / samples;
        }
    }

    public int CalculateBudgetCostForLOD(int level)
    {
        int totalCost = 0;

        float rateMultiplier = rateMultipliers[Mathf.Clamp(level, 0, rateMultipliers.Length - 1)];
        float lifeMultiplier = lifeMultipliers[Mathf.Clamp(level, 0, lifeMultipliers.Length - 1)];

        for (int i = 0; i < particleSystems.Length; i++)
        {
            var emission = emissionModules[i];

            // 基础发射率和寿命
            float rate = baseRates[i] * rateMultiplier;
            float lifetime = baseLifetimes[i] * lifeMultiplier;
            float baseCost = rate * lifetime;

            // Burst 成本
            int burstCost = 0;
            for (int j = 0; j < emission.burstCount; j++)
            {
                ParticleSystem.Burst burst = emission.GetBurst(j);
                // 考虑 burst 非 constant 的情况
                float value = GetRepresentativeValue(burst.count);
                burstCost += Mathf.RoundToInt(value);
            }

            float cost = baseCost + burstCost;

            // 附加模块，无脑分配权重
            float moduleCost = 1.0f;
            if (trailModules[i].enabled) moduleCost += 0.5f;
            if (particleSystems[i].collision.enabled) moduleCost += 0.3f;
            if (particleSystems[i].subEmitters.enabled) moduleCost += 0.8f;

            totalCost += Mathf.RoundToInt(cost * moduleCost);
        }

        return totalCost;
    }

}
