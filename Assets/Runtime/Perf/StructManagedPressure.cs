using System.Collections;
using UnityEngine;

public struct MySmallStruct
{
    public int a, b, c, d; // 16 bytes
}

public class StructManagedPressure : MonoBehaviour
{
    [SerializeField] int coldMB = 300;
    MySmallStruct[][] coldStructs;
    bool allocated;

    public void AllocateManagedStructs()
    {
        if (!gameObject.activeInHierarchy)
            return;

        StartCoroutine(UseMemory());
    }

    IEnumerator UseMemory()
    {
        if (allocated)
        {
            Debug.LogWarning("Managed struct memory already allocated.");
            yield break;
        }

        int chunkBytes = 1024 * 1024; // 1 MB
        int structSize = sizeof(int) * 4; // 16 bytes
        int countPerChunk = chunkBytes / structSize;

        coldStructs = new MySmallStruct[coldMB][];
        for (int i = 0; i < coldMB; i++)
        {
            var arr = new MySmallStruct[countPerChunk];

            // 每一页写一次，让页 dirty
            for (int j = 0; j < countPerChunk; j += 4096 / structSize)
            {
                arr[j].a = 1;
            }

            coldStructs[i] = arr;

            if (i % 8 == 0)
                yield return null;
        }

        allocated = true;
        Debug.Log($"[Managed] Struct memory allocated: {coldMB} MB (approx) - now idle.");
    }

    public void CleanUp()
    {
        if (!allocated)
        {
            Debug.LogWarning("No managed struct memory to free.");
            return;
        }

        coldStructs = null;
        allocated = false;

        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();

        Debug.Log("[Managed] Struct memory reference cleared, GC requested.");
    }
}
