using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GlobalParticleBudgetSystem : MonoBehaviour
{
    [SerializeField] int maxParticles = 8000;
    // 已占用预估
    int _currentEstimated;
    public static GlobalParticleBudgetSystem Instance { get; private set; }

    readonly List<BudgetedParticle> _actives = new List<BudgetedParticle>(128);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void Register(BudgetedParticle budgetedParticle)
    {
        if (_actives.Contains(budgetedParticle)) return;

    }
}
