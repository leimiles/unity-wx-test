# 微信小游戏内存问题与优化策略

本文结合项目现有的微信接入（`Assets/Settings/WXBuildSettings*.asset`、`Assets/WX-WASM-SDK-V2` 等）梳理小游戏在微信环境下的内存构成、常见风险、排查手段与优化路径，便于后续落地和复现。

## 运行时内存构成与天花板
- **Wasm 堆（Unity/IL2CPP）**：Player Settings 中的 WebGL Memory Size 即初始堆，运行中会扩容但受平台上限限制（不少设备在 256–512 MB 区间触顶）。堆越大启动越慢，占用越高。
- **JS 堆**：小游戏主域逻辑与 SDK 封装代码使用的堆，通常 <128 MB；持续创建对象或闭包会放大 GC 压力。
- **GPU 显存**：纹理解压后的大小、RenderTexture、Mesh/Buffer 都占用显存，移动端显存紧张且 OOM 会直接导致崩溃或黑屏。
- **文件系统缓存**：小游戏本地包与 CDN 缓存（~200 MB 左右硬限制），下载过多资源可能挤占可用空间。

实践上可将“稳定运行”目标控制在 **全局内存 <300 MB，显存保持足够余量**，并预留 15–25% 的安全空间给系统。

## 观测与定位手段
- **微信开发者工具**：性能面板查看内存曲线、显存占用，必要时抓取快照对比场景切换前后差异。
- **真机调试**：使用 `WX.TriggerGC()`（已由 SDK 暴露）在释放资源后尝试触发一次 JS 侧 GC，观察占用是否回落；持续偏高说明有泄漏或未卸载的资源。
- **Unity Profiler**：构建 Development Build 并在 `Assets/Settings/WXBuildSettings*.asset` 中打开 `devbuild/autoConnnectProfiler/profilingMemory`，通过设备网络连接 Profiler 观察 Managed/Native/Render 分类。
- **分模块 A/B**：按场景或功能逐步开关资源包，定位哪类资源推高堆/显存。

## 常见内存问题
- 纹理使用 RGBA32、开启 Read/Write 或未禁用不需要的 Mipmap，导致解压后体积暴涨。
- AudioClip 全量解压驻留内存；长音频未设为 Streaming，或在同一时刻创建多个 InnerAudioContext。
- AssetBundle/Addressables 未正确引用计数与卸载，场景切换后 `Resources.UnloadUnusedAssets()` 未调用。
- 高频 `new`、LINQ、字符串拼接或闭包在 Update/Coroutine 中产生 GC 抖动，导致堆膨胀与卡顿。
- RenderTexture、粒子与后处理链过多，显存被 RT/MRT 占满。
- WebGL Memory Size 设得过大导致启动慢、闲置内存高；设得过小则加载阶段 OOM。

## 优化策略（结合本项目可落地）
- **构建配置**
  - WebGL Memory Size 以“够用即止”原则起步（如 256 MB），在真机回放中观察是否触顶；必要时再小步调高。
  - 使用 IL2CPP + Managed Stripping（Medium/High）并移除未用模块，开启 Engine Stripping，降低 Wasm/内存占用。
  - 启用 Brotli 压缩与 `firstBundleCompress`（见 `WXBuildSettings`），首包体积与解压峰值会下降。
  - 关闭异常捕获扩展（Publishing Settings -> Enable Exceptions = Explicitly Thrown）减少代码尺寸与加载内存。

+- **资源侧**
  - 纹理：优先 ASTC/ETC2，控制分辨率，UI 用 SpriteAtlas 合图，关闭无用 Mipmap，取消 Read/Write。需读回 CPU 的贴图单独保留 RW。
  - Mesh/动画：合并静态物体，减少骨骼数和蒙皮权重，避免巨型网格；压缩 BlendShape。
  - 音频：BGM/长语音设为 Streaming；短音效使用 Vorbis/ADPCM，播放完成及时回收；限制同时播放的 InnerAudioContext 数量。
  - Shader/变体：剔除未用变体，压缩 Shader 包，减少加载时的内存峰值。
  - 资源分包：利用 CDN 首包（项目已支持 `firstBundleLoadingMethod = CDN`）将大资源拆分，按场景懒加载。

- **加载与生命周期管理**
  - Addressables/AssetBundle 设置引用计数，场景切换后调用 `Resources.UnloadUnusedAssets()`，并对不再使用的包执行 `AssetBundle.Unload(true)`。
  - 下载并发控制：同一时间限制 2–3 条 CDN 下载管线，避免短时堆/显存飙升。
  - 场景切换流程：卸载旧场景资源 -> 触发 `Resources.UnloadUnusedAssets()` -> 视情况调用 `WX.TriggerGC()` -> 再加载新场景，确保低谷足够低。

- **运行时分配与 GC**
  - 避免在 Update/Coroutine 中产生临时分配：替换 LINQ/foreach、`string.Format`、闭包；复用 `List`/`StringBuilder`。
  - 使用对象池复用特效、子弹、UI 节点，减少频繁实例化。
  - 纹理/RT 复用：统一尺寸的 RenderTexture 放入池，关闭未使用的摄像机与后处理链。

- **渲染与显存**
  - 降低屏幕分辨率或渲染比例（URP Render Scale），关闭多余 MSAA，减少同时活跃的高分辨率 RT。
  - 控制粒子数量与贴图尺寸；避免多个全屏后处理叠加。

- **文件系统与缓存**
  - 控制小游戏本地缓存体积，下载后按需清理旧版本资源；避免在小游戏 FS 中缓存无需长驻的临时文件。

## 建议的验证流程
1) 开启 Development Build + Profiler，跑关键场景并记录堆/显存峰值；微信开发者工具同步查看总内存。  
2) 逐步应用上述资源与加载优化（先纹理、再音频、再 Bundle 卸载），每步都抓一次快照验证下降幅度。  
3) 在真机低端设备上回归：进入大厅、战斗、返回菜单循环 10 分钟，确认内存曲线没有持续上升且显存稳定。  
4) 将通过的配置固化到 `WXBuildSettings` 与资源导入规则，纳入 CI 构建检查。

## 额外可用的代码片段
- 主动 GC（JS 侧，释放后尝试回收一次）：
  ```csharp
  #if UNITY_WEBGL
  WX.TriggerGC();
  #endif
  ```
- 简单内存日志（放在调试专用物体上）：
  ```csharp
  void Update()
  {
      var mono = System.GC.GetTotalMemory(false) / (1024f * 1024f);
      var rt = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
      if (Time.frameCount % 300 == 0)
          Debug.Log($"Memory (Mono): {mono:F1} MB, Reserved: {rt:F1} MB");
  }
  ```
  在微信端配合性能面板，可快速判断堆是否持续增长。
