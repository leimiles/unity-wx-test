using UnityEngine;

[DisallowMultipleComponent]
public class BudgetedParticles : MonoBehaviour
{
    [Header("Refs")]
    public ParticleSystem ps;

    [Header("Budget")]
    public VfxPriority priority = VfxPriority.Normal;
    public bool canBePausedWhenStarved = true;

    [Tooltip("估算单次在播成本（LOD0）。cost ≈ rateOverTime * lifetime + Σ(burstCounts）")]
    public int estimatedCostLOD0 = 600;

    [Header("LOD")]
    [Range(1, 4)] public int lodCount = 3; // 1~4
    [Tooltip("各 LOD 的发射率倍率，如 [1, 0.6, 0.35, 0.2]")]
    public float[] rateMultipliers = new float[] { 1f, 0.6f, 0.35f };
    [Tooltip("各 LOD 的寿命倍率，如 [1, 0.8, 0.6, 0.5]")]
    public float[] lifeMultipliers = new float[] { 1f, 0.8f, 0.6f };
    [Tooltip("是否在低 LOD 关闭 Trails/Collision 等高开销模块")]
    public bool[] disableTrailsAtLOD;

    [HideInInspector] public int lod = 0;

    // 缓存初始参数（避免运行时 GC）
    private float _baseRate;
    private float _baseLifetime;
    private bool _hasTrails;
    private ParticleSystem.TrailModule _trails;
    private ParticleSystem.EmissionModule _emission;
    private ParticleSystem.MainModule _main;

    void Reset()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Awake()
    {
        if (!ps) ps = GetComponent<ParticleSystem>();
        _emission = ps.emission;
        _main = ps.main;
        _baseLifetime = _main.startLifetime.constant;
        _baseRate = _emission.rateOverTime.constant;
        _trails = ps.trails;
        _hasTrails = _trails.enabled;

        // 防御：数组长度对齐
        if (rateMultipliers.Length < lodCount)
        {
            var t = new float[lodCount];
            for (int i = 0; i < lodCount; i++) t[i] = i < rateMultipliers.Length ? rateMultipliers[i] : 1f;
            rateMultipliers = t;
        }
        if (lifeMultipliers.Length < lodCount)
        {
            var t = new float[lodCount];
            for (int i = 0; i < lodCount; i++) t[i] = i < lifeMultipliers.Length ? lifeMultipliers[i] : 1f;
            lifeMultipliers = t;
        }
    }

    public int CostForLOD(int l)
    {
        if (l <= 0) return estimatedCostLOD0;
        float rm = rateMultipliers[l < rateMultipliers.Length ? l : rateMultipliers.Length - 1];
        float lm = lifeMultipliers[l < lifeMultipliers.Length ? l : lifeMultipliers.Length - 1];
        float cost = estimatedCostLOD0 * rm * lm;
        return Mathf.Max(1, Mathf.RoundToInt(cost));
    }

    public int CurrentCost() { return CostForLOD(lod); }

    public void ApplyLOD(int l)
    {
        lod = Mathf.Clamp(l, 0, lodCount - 1);
        float rm = rateMultipliers[lod];
        float lm = lifeMultipliers[lod];

        var rate = _emission.rateOverTime;
        rate.constant = _baseRate * rm;
        _emission.rateOverTime = rate;

        var lt = _main.startLifetime;
        lt.constant = _baseLifetime * lm;
        _main.startLifetime = lt;

        if (disableTrailsAtLOD != null && lod < disableTrailsAtLOD.Length)
        {
            bool disable = disableTrailsAtLOD[lod];
            _trails.enabled = _hasTrails && !disable;
        }
    }

    // 由全局缩放调用：只缩放发射率，保持视觉连续
    public void ApplyGlobalScale(float s)
    {
        var rate = _emission.rateOverTime;
        rate.constant = _baseRate * rateMultipliers[lod] * s;
        _emission.rateOverTime = rate;
    }

    public void Play(Vector3 pos, Quaternion rot, Transform parent = null)
    {
        transform.SetPositionAndRotation(pos, rot);
        if (parent) transform.SetParent(parent, false);

        // 恢复到配置 LOD（有可能上次被降级）
        ApplyLOD(lod);

        // 申请预算
        if (GlobalParticleBudget.I != null)
        {
            if (!GlobalParticleBudget.I.TryAcquire(this))
            {
                // 超限：直接拒绝，不播
                gameObject.SetActive(false);
                return;
            }
            GlobalParticleBudget.I.Register(this);
        }

        gameObject.SetActive(true);
        ps.Play(true);
        // 自动回收
        StopAllCoroutines();
        StartCoroutine(_WaitAndReturn());
    }

    // 更详细的成本计算
    public int CalculateDetailedCost()
    {
        var main = ps.main;
        var emission = ps.emission;

        // 基础粒子成本
        float baseCost = emission.rateOverTime.constant * main.startLifetime.constant;

        // 模块成本加成
        float moduleCost = 1f;
        if (ps.trails.enabled) moduleCost += 0.5f;
        if (ps.collision.enabled) moduleCost += 0.3f;
        if (ps.subEmitters.enabled) moduleCost += 0.8f;

        return Mathf.RoundToInt(baseCost * moduleCost);
    }

    private System.Collections.IEnumerator _WaitAndReturn()
    {
        while (ps.IsAlive(true)) yield return null;
        // 回收
        if (GlobalParticleBudget.I != null) GlobalParticleBudget.I.Unregister(this);
        gameObject.SetActive(false);
    }

    // 被全局管理器调用做“淘汰”
    public void PauseAndHide()
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (GlobalParticleBudget.I != null) GlobalParticleBudget.I.Unregister(this);
        gameObject.SetActive(false);
    }
}
