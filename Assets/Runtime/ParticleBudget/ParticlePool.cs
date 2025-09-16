using UnityEngine;
using System.Collections.Generic;

public class ParticlePool : MonoBehaviour
{
    public static ParticlePool I;

    [System.Serializable]
    public class Entry
    {
        public string id;
        public BudgetedParticles prefab;
        public int prewarm = 4;
    }

    public Entry[] entries;
    private readonly Dictionary<string, Queue<BudgetedParticles>> _pool = new Dictionary<string, Queue<BudgetedParticles>>(64);

    void Awake()
    {
        I = this;
        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            var q = new Queue<BudgetedParticles>(e.prewarm + 2);
            _pool[e.id] = q;
            for (int k = 0; k < e.prewarm; k++)
            {
                var inst = Instantiate(e.prefab, transform);
                inst.gameObject.SetActive(false);
                q.Enqueue(inst);
            }
        }
    }

    public BudgetedParticles Spawn(string id, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (!_pool.TryGetValue(id, out var q)) return null;
        BudgetedParticles inst = q.Count > 0 ? q.Dequeue() : Instantiate(entries[FindIndex(id)].prefab, transform);
        inst.gameObject.SetActive(true);
        inst.Play(pos, rot, parent);
        // 回收由 BudgetedParticles 自己做，回到 inactive 后放回队列
        StartCoroutine(_WaitInactiveAndReturn(id, inst));
        return inst;
    }

    private System.Collections.IEnumerator _WaitInactiveAndReturn(string id, BudgetedParticles inst)
    {
        // 等待被 BudgetedParticles 设为 inactive
        while (inst.gameObject.activeSelf) yield return null;
        if (_pool.TryGetValue(id, out var q)) q.Enqueue(inst);
    }

    private int FindIndex(string id)
    {
        for (int i = 0; i < entries.Length; i++) if (entries[i].id == id) return i;
        return 0;
    }
}
