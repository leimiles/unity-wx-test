# Performance Optimizations

This document describes the performance optimizations applied to the Unity WeChat mini-game project.

## Overview

The optimizations focus on reducing allocations, minimizing lock contention, caching expensive operations, and providing conditional logging to improve runtime performance, especially for the WeChat mini-game platform which has strict memory and CPU constraints.

## Optimizations Applied

### 1. Bootstrap.cs - Subsystem Registration Optimization

**Issue**: `List.Exists()` with lambda was used in the hot path for checking duplicate subsystem names, causing O(n) linear search and lambda allocation on each call.

**Solution**: 
- Added `HashSet<string> _subSystemNames` for O(1) duplicate detection
- Replaced `_subSystems.Exists(s => s.Name == name)` with `!_subSystemNames.Add(name)`
  - Note: `HashSet.Add()` returns `false` when item already exists, so `!Add()` detects duplicates
- Added conditional compilation `BOOTSTRAP_DETAILED_LOGGING` to control verbose logging

**Impact**: 
- O(n) → O(1) lookup complexity
- Zero lambda allocations during registration
- Reduced string interpolation overhead in Release builds

**Code Location**: `Assets/Runtime/Boot/Bootstrap.cs`

### 2. PredefinedAssemblyUtil.cs - Reflection Caching

**Issue**: `AppDomain.CurrentDomain.GetAssemblies()` and `assembly.GetTypes()` were called on every `GetTypes()` invocation, causing expensive reflection operations.

**Solution**:
- Added static cache using `Lazy<Dictionary<AssemblyType, Type[]>>` for thread-safe initialization
- Implemented `BuildAssemblyTypeCache()` for one-time reflection scanning
- Using `Lazy<T>` provides cleaner and more efficient thread-safety than double-check locking

**Impact**:
- Reflection only happens once during initialization
- Subsequent calls are pure dictionary lookups
- Thread-safe for concurrent access with minimal overhead

**Code Location**: `Assets/Runtime/EventBus/PredefinedAssemblyUtil.cs`

### 3. ModularBoneSystem.cs - Transform Array Caching

**Issue**: `GetComponentsInChildren<Transform>()` was called every time bones were verified, allocating a new array each time.

**Solution**:
- Added `_boneTransformCache` field to store the Transform array
- Added `_lastBonesRoot` to track which root the cache is for
- Only calls `GetComponentsInChildren<Transform>()` when the root bone changes
- Reuses cached array when verifying the same bone root multiple times

**Impact**:
- Reduced memory allocations during bone verification
- Faster bone system re-verification for the same character
- Cache automatically invalidates when switching to different bone roots

**Code Location**: `Assets/Runtime/ModularsCharacter/ModularBoneSystem.cs`

### 4. EventBus.cs - ArrayPool Threshold Optimization

**Issue**: ArrayPool was used for all collection sizes, even small ones where the pool overhead might exceed the allocation cost.

**Solution**:
- Added `STACK_ALLOC_THRESHOLD = 8` constant for future optimization
- Documented ArrayPool strategy in class comments

**Impact**:
- Prepared for future stack allocation optimization for small collections
- Documented performance strategy for maintainers

**Code Location**: `Assets/Runtime/EventBus/EventBus.cs`

### 5. YooService.cs - Lock Contention Documentation & Logging Optimization

**Issue**: 
- Async locking strategy was not clearly documented
- Verbose logging in hot paths caused string allocation overhead

**Solution**:
- Added performance comments explaining minimal lock holding time
- Added conditional compilation `YOOSERVICE_DETAILED_LOGGING` for verbose logs
- Documented async SemaphoreSlim usage pattern

**Impact**:
- Clearer understanding of concurrency model
- Reduced string allocations in Release builds
- Better maintainability

**Code Location**: `Assets/Runtime/YooUtils/YooService.cs`

### 6. GameManager.cs - Concurrency Documentation

**Issue**: Flow switching concurrency control was not well documented.

**Solution**:
- Added class-level documentation explaining Flow management
- Documented lock usage and cancellation strategy
- Explained Fire-and-Forget pattern with state protection

**Impact**:
- Improved code maintainability
- Clearer understanding of thread safety

**Code Location**: `Assets/Runtime/GameManager/GameManager.cs`

## Conditional Compilation Flags

Two conditional compilation symbols are defined to control logging verbosity:

### BOOTSTRAP_DETAILED_LOGGING
Controls detailed logging in `Bootstrap.cs`. To disable for Release builds:
1. Remove the `#define BOOTSTRAP_DETAILED_LOGGING` line in Bootstrap.cs, or
2. Add `BOOTSTRAP_DETAILED_LOGGING` to "Scripting Define Symbols" in Player Settings for Debug builds only

### YOOSERVICE_DETAILED_LOGGING
Controls detailed logging in `YooService.cs`. Same approach as above.

**Recommendation**: Keep these enabled during development, disable for production builds to reduce:
- String allocation overhead
- String interpolation CPU cost
- Log processing overhead

## Performance Best Practices

Based on these optimizations, here are recommended practices for this codebase:

### 1. Collection Lookups
- Use `HashSet<T>` or `Dictionary<K,V>` for frequent lookups instead of `List<T>.Contains()` or `List<T>.Exists()`
- Time complexity: O(1) vs O(n)

### 2. Reflection
- Cache reflection results (types, methods, properties) at initialization
- Never perform reflection in Update/FixedUpdate or other hot paths

### 3. Component Queries
- Cache `GetComponent<T>()` and `GetComponentsInChildren<T>()` results when possible
- Use `TryGetComponent<T>()` to avoid exceptions
- Prefer manual references over FindObjectOfType in hot paths

### 4. String Operations
- Use conditional compilation for debug/verbose logging
- Avoid string interpolation in hot paths
- Consider StringBuilder for repeated concatenations

### 5. Async/Threading
- Use SemaphoreSlim for async locking (not lock/Monitor)
- Minimize lock holding time
- Use async/await pattern consistently

### 6. Memory Management
- Use ArrayPool<T> for temporary arrays (size > 8 elements recommended)
- Consider object pooling for frequently created/destroyed objects
- Cache Transform and component references

### 7. EventBus Usage
- The EventBus already uses ArrayPool and snapshot patterns
- Avoid heavy operations in event handlers
- Consider event handler exceptions don't propagate to other handlers

## Measuring Performance

To verify these optimizations:

1. **Unity Profiler**:
   - Check CPU usage in Bootstrap initialization
   - Monitor GC.Alloc in event dispatch
   - Verify reduced allocation in asset loading

2. **Memory Profiler**:
   - Check managed heap allocations
   - Verify reduced temporary object allocations

3. **WeChat DevTools**:
   - Monitor startup time
   - Check memory usage in WeChat mini-game environment
   - Use `WX.GetDynamicMemorySize()` to track runtime memory

## Future Optimization Opportunities

Areas that could be optimized further if performance issues arise:

1. **Singleton Pattern**: Consider replacing `FindAnyObjectByType<T>()` with a registry pattern if lookups become frequent
2. **EventBus**: Implement stack allocation for small event handler collections (<= 8 handlers)
3. **Asset Loading**: Batch asset loading operations to reduce individual async overhead
4. **Scene Loading**: Implement scene preloading and background loading strategies
5. **Particle System**: Implement the GlobalParticleBudgetSystem (currently empty)

## Migration Notes

These optimizations are **backward compatible** and require no changes to calling code. The APIs remain the same; only internal implementation has been optimized.

To benefit from conditional logging optimization:
- Ensure `BOOTSTRAP_DETAILED_LOGGING` and `YOOSERVICE_DETAILED_LOGGING` are NOT defined in release builds
- Consider adding these as platform-specific defines in build pipeline

## References

- [Unity Best Practices](https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity.html)
- [WeChat Mini Game Performance](https://developers.weixin.qq.com/minigame/en/dev/guide/performance/)
- [C# Performance Tips](https://docs.microsoft.com/en-us/dotnet/csharp/advanced-topics/performance/)
- [Unity Memory Management](https://docs.unity3d.com/Manual/performance-memory-overview.html)

---

Last Updated: 2025-12-27
