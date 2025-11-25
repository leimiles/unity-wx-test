using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ColdMemoryMaker : MonoBehaviour
{
    [SerializeField] int coldMB = 300;
    byte[][] coldBuffers;
    bool allocated = false;

    public IEnumerator UseMemory()
    {
        if (allocated)
        {
            Debug.LogWarning("Cold memory already allocated.");
            yield break;
        }

        int chunk = 1024 * 1024;   // 1 MB
        coldBuffers = new byte[coldMB][];

        for (int i = 0; i < coldMB; i++)
        {
            coldBuffers[i] = new byte[chunk];

            // make page dirty
            for (int j = 0; j < chunk; j += 4096)
            {
                coldBuffers[i][j] = 1;
            }

            if (i % 8 == 0)
            {
                yield return null;
            }
        }

        allocated = true;
        Debug.Log($"Cold memory allocated: {coldMB} MB - now idle.");
    }

    public void CleanUp()
    {
        if (!allocated)
        {
            Debug.LogWarning("No cold memory to free.");
            return;
        }

        // 1. 断开托管引用
        coldBuffers = null;
        allocated = false;

        // 2. 触发 GC 方便测试
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();

        Debug.Log("Cold memory reference cleared, GC requested.");
    }

    public void AllocateMemory()
    {
        StartCoroutine(UseMemory());
    }

}
