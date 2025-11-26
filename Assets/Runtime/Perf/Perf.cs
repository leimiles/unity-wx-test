using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEngine;
#if UNITY_WEBGL
using WeChatWASM;
#endif

using System.Runtime.InteropServices;


[DisallowMultipleComponent]
public class Perf : MonoBehaviour
{
    void Start()
    {
#if UNITY_WEBGL
        WX.ReportGameStart();
#endif
    }

    public void PeekMemory()
    {
        float reservedRam = Profiler.GetTotalReservedMemoryLong();
        Debug.Log($"[Unity]Total Reserved Memory: {reservedRam / 1048576f:F1} MB");

        float totalMemory = Profiler.GetTotalAllocatedMemoryLong();
        Debug.Log($"[Unity]Total Allocated Memory: {totalMemory / 1048576f:F1} MB");

        // 作为 Memory Profiler 中的 Managed 部分的 Reserved，不包含 VM 内部开销
        float monoHeapSize = Profiler.GetMonoHeapSizeLong();
        Debug.Log($"[Unity]Mono Heap Size: {monoHeapSize / 1048576f:F1} MB");
        // 作为 Memory Profiler 中的 Managed 部分的 Object，不包含 VM 内部开销
        float monoRam = Profiler.GetMonoUsedSizeLong();
        Debug.Log($"[Unity]Mono Used Size: {monoRam / 1048576f:F1} MB");

        // 作为 Memory Profiler 中的 Graphics（Estimated）
        float graphicsMemory = Profiler.GetAllocatedMemoryForGraphicsDriver();
        Debug.Log($"[Unity]Graphics Driver Allocated Memory: {graphicsMemory / 1048576f:F1} MB");
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern int GetWasmTotal();
    [DllImport("__Internal")] private static extern int GetWasmUsed();
    [DllImport("__Internal")] private static extern double GetJSHeapUsedWX();
    [DllImport("__Internal")] private static extern double GetJSHeapTotalWX();
#endif
    public void PeekWasmMemory()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // ---------- WASM ----------
        int wasmTotal = GetWasmTotal();
        int wasmUsed  = GetWasmUsed();

        Debug.Log($"[WASM] TotalHeapMemory : {wasmTotal / 1048576f:F1} MB");
        Debug.Log($"[WASM] DynamicMemory   : {wasmUsed  / 1048576f:F1} MB");
        Debug.Log($"[WASM] Unallocated     : {(wasmTotal - wasmUsed) / 1048576f:F1} MB");

        // ---------- JS Heap 暂时拿不到，基本都是 30 - 50 MB 左右----------
        //double jsUsed = GetJSHeapUsedWX();
        //double jsTot  = GetJSHeapTotalWX();

        //Debug.Log($"[JS]   Heap Used       : {jsUsed / 1048576.0:F1} MB");
        //Debug.Log($"[JS]   Heap Total      : {jsTot  / 1048576.0:F1} MB");
#else
        Debug.Log("WASM/JS Heap probe only works in WebGL builds.");
#endif

        Debug.Log("==================================");
    }



}
