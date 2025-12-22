# 角色 3C 系统设计文档

## 一、总体设计目标

### 1.1 核心原则

1. **输入无关性**
   - 键盘 / 手柄 / 触控 → 统一抽象为玩家意图
   - 任何新设备都不应影响运动学与相机逻辑

2. **运动权威清晰**
   - 角色位移与旋转只能通过 Motor 这一条通道发生
   - 动画、输入、相机均不得直接改 Transform 位移
   - **Motor 不做规则判断**：Controller 决定"能不能做"，Motor 只负责"怎么做"
   - 禁止在 Motor 中写业务规则（如 `if (IsGrounded && CanJump)`），规则应下沉到 Controller

3. **表现与规则分离**
   - 动画（Playables）是表现层，不是规则层
   - 跳不跳、能不能转向、速度多少由 Controller/Motor 决定

4. **可替换、可扩展**
   - CharacterController ↔ Rigidbody
   - AnimatorController ↔ Playables
   - 自研 Camera ↔ Cinemachine
   - 都是"换实现，不换接口"

### 1.2 设计宣言

> **输入是意图，控制是规则，运动是权威，动画是表现，相机是响应。**
>
> **任何系统只做一件事，并且只能向下依赖。**

---

## 二、分层架构

### 2.1 依赖方向（严格单向）

```
┌─────────────────┐
│  Input Source   │  ← 设备层（键盘/手柄/触控）
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│ PlayerCommand   │  ← 意图层（统一协议）
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│ThirdPersonCtrl  │  ← 规则层（编排/决策）
└────────┬────────┘
         │
         ├──────────┬──────────┬──────────┐
         ↓          ↓          ↓          ↓
    ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐
    │ Motor  │ │ Camera │ │Animation│ │ State  │
    └────────┘ └────────┘ └────────┘ └────────┘
    执行层      响应层      表现层      状态层
```

### 2.2 禁止的反向依赖

- ❌ Motor 读取 InputAction
- ❌ Animation 直接改 transform.position
- ❌ Camera 决定角色能不能移动
- ❌ Controller 直接操作 Transform
- ❌ **Motor 做规则判断**（如 `if (IsGrounded && CanJump)`），规则应下沉到 Controller

---

## 三、核心接口设计

### 3.1 输入层接口

#### `IInputSource`
输入源抽象接口，负责将设备输入归一化为统一格式。

```csharp
/// <summary>
/// 输入源接口 - 负责将设备输入转换为 PlayerCommand
///
/// 错误处理：
/// - ReadCommand() 异常时应返回默认的 PlayerCommand，不应抛出异常
/// - 输入设备断开时应返回空命令，不应崩溃
/// </summary>
public interface IInputSource
{
    /// <summary>当前控制方案（键盘鼠标/手柄/触控）</summary>
    ControlScheme CurrentScheme { get; }

    /// <summary>读取当前帧的玩家命令</summary>
    /// <remarks>
    /// 如果读取失败，应返回默认的 PlayerCommand（所有字段为默认值）
    /// 不应抛出异常，避免影响整个系统
    /// </remarks>
    PlayerCommand ReadCommand();

    /// <summary>是否启用</summary>
    bool IsEnabled { get; set; }
}
```

#### `PlayerCommand`
玩家意图的统一数据结构。

**按钮语义说明**：
- **Pressed**：本帧按下（按下沿），用于触发一次性动作（如跳跃、交互）
- **Held**：是否持续按住，用于持续状态（如冲刺、瞄准）
- 明确语义可避免：连跳 bug、触控误触、手柄重复触发等问题

**消费语义说明**：
- **Consumed**：标记该命令已被消费，避免多系统重复处理
- **⚠️ 实现约束**：`PlayerCommand` 是 `struct`（值类型），`Consumed` 标记仅在 Controller 内部使用（局部变量）
- Controller 处理命令后，在内部标记为已消费，并通过事件/路由器对外暴露已消耗的动作
- **禁止**：依赖 struct 的可变副本实现跨系统消费共享
- **若需要多系统竞争同一输入**：使用 `class` 或引入 `CommandContext`（集中式路由器）来保证消费可见性

**Consumed 字段设计说明**：
- `JumpConsumed` 和 `InteractConsumed` 字段在 struct 中是为了完整性
- **实际使用中**：Controller 内部使用局部变量 `cmd`，这些字段仅作为内部标记使用
- **外部系统不应读取这些字段**：应通过事件 `OnJumpConsumed` / `OnInteractConsumed` 获取消费信息
- **如果未来需要多系统竞争同一输入**：考虑将 `PlayerCommand` 改为 `class`（引用语义）或引入 `CommandContext`（集中式路由器）

```csharp
/// <summary>
/// 玩家命令 - 统一的意图协议
///
/// 按钮语义：
/// - Pressed：本帧按下（按下沿），用于一次性动作
/// - Held：持续按住，用于持续状态
///
/// 消费语义：
/// - Consumed：标记命令已被消费，避免多系统重复处理
/// - ⚠️ 实现约束：Consumed 标记仅在 Controller 内部使用（局部变量）
/// - 若需要跨系统共享消费结果，必须通过事件/路由器转发，禁止依赖 struct 的可变副本
/// </summary>
public struct PlayerCommand
{
    /// <summary>移动输入（归一化的 Vector2，范围 [-1, 1]）</summary>
    public Vector2 Move;

    /// <summary>视角输入（归一化的 Vector2，范围 [-1, 1]）</summary>
    public Vector2 Look;

    // ========== 一次性动作（Pressed） ==========

    /// <summary>跳跃按钮 - 本帧按下（按下沿）</summary>
    public bool JumpPressed;

    /// <summary>跳跃按钮 - 是否已被消费</summary>
    public bool JumpConsumed;

    /// <summary>交互按钮 - 本帧按下（按下沿）</summary>
    public bool InteractPressed;

    /// <summary>交互按钮 - 是否已被消费</summary>
    public bool InteractConsumed;

    // ========== 持续状态（Held） ==========

    /// <summary>冲刺按钮 - 是否持续按住</summary>
    public bool SprintHeld;

    /// <summary>瞄准按钮 - 是否持续按住</summary>
    public bool AimHeld;

    // ========== 元数据 ==========

    /// <summary>当前输入设备方案</summary>
    public ControlScheme ControlScheme;

    /// <summary>时间戳（用于调试）</summary>
    public float Timestamp;
}
```

#### `ControlScheme`
控制方案枚举。

```csharp
/// <summary>
/// 控制方案类型
/// </summary>
public enum ControlScheme
{
    KeyboardMouse,
    Gamepad,
    Touch
}
```

---

### 3.2 控制层接口

#### `IPlayerController`
玩家控制器接口，负责规则编排和决策。

```csharp
/// <summary>
/// 玩家控制器接口 - 规则中枢，负责编排和决策
///
/// 错误处理：
/// - SetInputSource() 等设置方法应进行空值检查
/// - Tick() 应捕获异常并记录，不应抛出异常
/// - SetMode() 失败时应回滚到原模式或默认模式
/// </summary>
public interface IPlayerController
{
    /// <summary>当前玩家模式</summary>
    PlayerMode CurrentMode { get; }

    /// <summary>当前运动策略</summary>
    MotionPolicy CurrentMotionPolicy { get; }

    /// <summary>更新控制器（每帧调用）</summary>
    /// <remarks>
    /// 应捕获异常并记录，不应抛出异常，避免单帧错误影响整个系统
    /// </remarks>
    void Tick(float deltaTime);

    /// <summary>设置输入源</summary>
    /// <remarks>
    /// 应进行空值检查，如果为 null 应抛出 ArgumentNullException
    /// </remarks>
    void SetInputSource(IInputSource inputSource);

    /// <summary>设置运动系统</summary>
    /// <remarks>
    /// 应进行空值检查，如果为 null 应抛出 ArgumentNullException
    /// </remarks>
    void SetMotor(ICharacterMotor motor);

    /// <summary>设置相机系统</summary>
    /// <remarks>
    /// 应进行空值检查，如果为 null 应抛出 ArgumentNullException
    /// </remarks>
    void SetCameraRig(ICameraRig cameraRig);

    /// <summary>设置动画系统</summary>
    /// <remarks>
    /// 应进行空值检查，如果为 null 应抛出 ArgumentNullException
    /// </remarks>
    void SetAnimationDriver(IAnimationDriver animationDriver);

    /// <summary>切换玩家模式</summary>
    /// <remarks>
    /// 如果切换失败，应回滚到原模式或默认模式
    /// </remarks>
    void SetMode(PlayerMode mode);

    /// <summary>获取当前状态（供其他系统查询）</summary>
    ControllerState GetState();
}
```

#### `PlayerMode`
玩家模式枚举。

```csharp
/// <summary>
/// 玩家模式 - 决定行为规则
/// </summary>
public enum PlayerMode
{
    Locomotion,  // 正常移动
    Aim,         // 瞄准模式
    Vehicle,     // 载具模式
    LockOn,      // 锁定目标模式
    Climbing,    // 攀爬模式
    Swimming     // 游泳模式
}
```

#### `MotionPolicy`
运动策略枚举。

```csharp
/// <summary>
/// 运动策略 - 决定位移权威
/// </summary>
public enum MotionPolicy
{
    /// <summary>Motor 完全控制位移</summary>
    MotorAuthority,

    /// <summary>RootMotion 完全控制位移</summary>
    RootMotionAuthority,

    /// <summary>混合模式：RootMotion 提供基础位移，Motor 处理碰撞</summary>
    Hybrid,

    /// <summary>Motor 控制位移，RootMotion 控制旋转</summary>
    MotorWithRootRotation
}
```

#### `ControllerState`
控制器状态数据。

```csharp
/// <summary>
/// 控制器状态 - 供其他系统查询
/// </summary>
public struct ControllerState
{
    public PlayerMode Mode;
    public MotionPolicy MotionPolicy;
    public bool CanMove;
    public bool CanRotate;
    public bool CanJump;
    public float MaxSpeed;
    public float TurnSpeed;
}
```

---

### 3.3 运动层接口

#### `ICharacterMotor`
角色运动接口，位移和旋转的唯一权威。

**核心职责原则**：
- Motor **不做业务规则判断**，只执行 Controller 已批准的行为
- Controller 负责判断"能不能跳"、"能不能移动"（业务规则），Motor 只负责"怎么执行"（物理计算）
- **允许 Motor 做物理安全夹紧**，但不做业务规则判断：
  - ✅ **允许**：`jumpForce = Mathf.Max(jumpForce, 0)`（防止负值）
  - ✅ **允许**：`velocity.y = Mathf.Max(velocity.y, -maxFallSpeed)`（防止速度爆掉）
  - ✅ **允许**：`speed = Mathf.Clamp(speed, 0, maxSpeed)`（防止超速）
  - ❌ **禁止**：`if (IsGrounded && CanJump)`（业务规则判断）
  - ❌ **禁止**：`if (CurrentMode == PlayerMode.Locomotion)`（业务规则判断）
- **边界说明**：
  - Motor 的防御性检查是为了**物理安全**（防止数值异常、NaN、Infinity）
  - Motor 的防御性检查**不是**为了重新判断业务规则（Controller 已经判断过了）
  - 避免两种极端：
    - ❌ Motor 完全不防御 → 某帧 Controller 状态异常导致速度爆掉
    - ❌ Motor 重新写规则 → 破坏分层原则

**接口方法分类**：

1. **意图输入**（持续性的运动意图）：
   - `SetMoveIntent()` - 设置移动方向和速度
   - `SetRotationIntent()` - 设置期望朝向

2. **行为触发**（一次性的动作执行）：
   - `Jump()` - 执行跳跃
   - `ApplyRootMotion()` - 应用 RootMotion 增量

3. **配置控制**：
   - `SetMotionPolicy()` - 设置运动策略

```csharp
/// <summary>
/// 角色运动接口 - 位移和旋转的唯一权威
///
/// 职责边界：
/// - Motor **不做业务规则判断**，只执行 Controller 已批准的行为
/// - Controller 决定"能不能做"（业务规则），Motor 决定"怎么做"（物理计算）
/// - **允许 Motor 做物理安全夹紧**，但不做业务规则判断：
///   - ✅ 允许：数值范围限制（jumpForce = Max(jumpForce, 0)）
///   - ✅ 允许：速度上限保护（velocity.y = Max(velocity.y, -maxFallSpeed)）
///   - ❌ 禁止：业务规则判断（if (IsGrounded && CanJump)）
/// - 避免两种极端：
///   - ❌ Motor 完全不防御 → 某帧 Controller 状态异常导致速度爆掉
///   - ❌ Motor 重新写规则 → 破坏分层原则
///
/// ⚠️ 时序约束：
/// - SetMoveIntent() / SetRotationIntent() 必须在 Tick() 之前调用
/// - 错误的顺序会导致"输入延迟一帧"的手感问题
/// - 参见 Frame Contract（帧契约）章节了解详细更新顺序
/// </summary>
public interface ICharacterMotor
{
    /// <summary>是否启用</summary>
    bool IsEnabled { get; set; }

    /// <summary>当前速度</summary>
    Vector3 Velocity { get; }

    /// <summary>是否在地面</summary>
    bool IsGrounded { get; }

    /// <summary>是否在移动</summary>
    bool IsMoving { get; }

    /// <summary>当前运动策略</summary>
    MotionPolicy CurrentPolicy { get; }

    /// <summary>更新运动（每帧调用）</summary>
    /// <remarks>
    /// ⚠️ 必须在 SetMoveIntent() / SetRotationIntent() 之后调用
    /// 参见 Frame Contract（帧契约）章节了解详细更新顺序
    /// 应捕获异常并记录，不应抛出异常，避免物理计算错误影响整个系统
    /// </remarks>
    void Tick(float deltaTime);

    // ========== 意图输入（持续性） ==========

    /// <summary>设置移动意图（来自 Controller）</summary>
    /// <remarks>
    /// ⚠️ 必须在 Tick() 之前调用，否则会导致输入延迟一帧
    /// </remarks>
    void SetMoveIntent(Vector3 moveDirection, float speed);

    /// <summary>设置旋转意图（来自 Controller）</summary>
    /// <remarks>
    /// ⚠️ 必须在 Tick() 之前调用，否则会导致输入延迟一帧
    /// </remarks>
    void SetRotationIntent(Quaternion rotation);

    // ========== 行为触发（一次性） ==========

    /// <summary>执行跳跃（Controller 已判断可以跳跃）</summary>
    /// <remarks>
    /// Controller 已判断可以跳跃，Motor 只负责执行物理计算。
    /// Motor 可以做物理安全夹紧（如 jumpForce = Max(jumpForce, 0)），但不做业务规则判断。
    /// </remarks>
    void Jump(float jumpForce);

    /// <summary>应用 RootMotion 位移（当策略为 RootMotionAuthority 时）</summary>
    void ApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation);

    // ========== 配置控制 ==========

    /// <summary>设置运动策略</summary>
    void SetMotionPolicy(MotionPolicy policy);

    /// <summary>获取当前状态（供其他系统查询）</summary>
    MotorState GetState();
}
```

#### `MotorState`
运动状态数据。

```csharp
/// <summary>
/// 运动状态 - 供其他系统查询
/// </summary>
public struct MotorState
{
    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 GroundNormal;
    public bool IsGrounded;
    public bool IsMoving;
    public float GroundDistance;
    public float Speed;
}
```

---

### 3.4 相机层接口

#### `ICameraRig`
相机接口，负责视角控制。

**错误处理**：
- `CameraTransform` 或 `Camera` 可能为 null（组件未初始化），调用方应检查
- `Tick()` 应捕获异常，不应抛出异常
- `SetTarget()` 为 null 时应重置到默认跟随目标

```csharp
/// <summary>
/// 相机接口 - 视角响应器
///
/// 错误处理：
/// - CameraTransform 或 Camera 可能为 null（组件未初始化），调用方应检查
/// - Tick() 应捕获异常，不应抛出异常
/// - SetTarget() 为 null 时应重置到默认跟随目标
/// </summary>
public interface ICameraRig
{
    /// <summary>是否启用</summary>
    bool IsEnabled { get; set; }

    /// <summary>相机 Transform（可能为 null）</summary>
    Transform CameraTransform { get; }

    /// <summary>Unity Camera 组件（可能为 null）</summary>
    Camera Camera { get; }

    /// <summary>更新相机（每帧调用）</summary>
    /// <remarks>
    /// 应捕获异常并记录，不应抛出异常
    /// </remarks>
    void Tick(float deltaTime);

    /// <summary>设置视角输入（来自 Controller）</summary>
    void SetLookInput(Vector2 lookInput);

    /// <summary>设置跟随目标</summary>
    /// <remarks>
    /// 如果 target 为 null，应重置到默认跟随目标
    /// </remarks>
    void SetTarget(Transform target);

    /// <summary>设置相机参数（根据模式调整）</summary>
    void SetCameraParams(CameraParams parameters);

    /// <summary>重置相机到默认位置</summary>
    void ResetCamera();
}
```

#### `CameraBlendMode`
相机插值策略枚举。

```csharp
/// <summary>
/// 相机插值策略 - 用于模式切换时的相机过渡
/// </summary>
public enum CameraBlendMode
{
    /// <summary>立即切换，无插值</summary>
    Instant,

    /// <summary>平滑插值（线性）</summary>
    Smooth,

    /// <summary>缓入缓出插值（EaseInOut）</summary>
    EaseInOut
}
```

#### `CameraParams`
相机参数配置。

**插值策略说明**：
- `BlendMode`：控制相机参数切换时的插值方式
- `BlendDuration`：插值持续时间（秒），仅在 `BlendMode != Instant` 时生效
- 为 Aim / LockOn / Vehicle 等模式切换提供平滑过渡

```csharp
/// <summary>
/// 相机参数配置
///
/// 插值策略：
/// - BlendMode：控制参数切换时的插值方式
/// - BlendDuration：插值持续时间，用于平滑过渡
/// </summary>
public struct CameraParams
{
    // ========== 基础参数 ==========

    public float FOV;
    public float Distance;
    public float Height;
    public Vector3 Offset;
    public float Sensitivity;
    public float PitchMin;
    public float PitchMax;
    public bool InvertY;

    // ========== 插值策略 ==========

    /// <summary>相机参数插值策略</summary>
    public CameraBlendMode BlendMode;

    /// <summary>插值持续时间（秒），仅在 BlendMode != Instant 时生效</summary>
    public float BlendDuration;
}
```

---

### 3.5 动画层接口

#### `IAnimationDriver`
动画驱动接口，负责动画表现。

**OneShot 动画控制权返回**：
- `IsPlayingOneShot`：查询当前是否正在播放 OneShot 动画
- `OnOneShotFinished`：OneShot 动画完成时触发的事件，携带 `animationName` 参数
- Controller 可以安全地在动画结束后切换 Mode，无需依赖"魔法时间"（硬编码时间值）
- **⚠️ 事件管理**：Controller 在 Enter/Exit 模式时必须解除订阅，避免事件泄漏或重复订阅

```csharp
/// <summary>
/// 动画驱动接口 - 动画表现层
///
/// 错误处理：
/// - PlayOneShot() 如果动画不存在，应记录警告但不抛出异常
/// - Tick() 应捕获异常，不应抛出异常
/// - 动画组件缺失时应降级处理，不影响其他系统
/// </summary>
public interface IAnimationDriver
{
    /// <summary>是否启用</summary>
    bool IsEnabled { get; set; }

    /// <summary>更新动画（每帧调用）</summary>
    /// <remarks>
    /// 应捕获异常并记录，不应抛出异常
    /// </remarks>
    void Tick(float deltaTime);

    /// <summary>设置运动状态（来自 Motor）</summary>
    void SetMotorState(MotorState state);

    /// <summary>设置控制器状态（来自 Controller）</summary>
    void SetControllerState(ControllerState state);

    // ========== OneShot 动画控制 ==========

    /// <summary>播放一次性动画（如翻滚、攀爬）</summary>
    /// <param name="animationName">动画名称（用于事件回调识别）</param>
    /// <param name="transitionTime">过渡时间</param>
    /// <returns>true 表示播放成功，false 表示失败</returns>
    /// <remarks>
    /// 返回 false 的可能原因：
    /// - 动画不存在（应记录警告日志："Animation not found: {animationName}"）
    /// - 当前正在播放其他 OneShot 动画（应记录信息日志："OneShot busy: {currentAnimation}, requested: {animationName}"）
    ///
    /// 并发仲裁规则（由实现类决定）：
    /// - 当 Roll 正在播放时，Vault 请求来了，可以选择：
    ///   - 拒绝（返回 false，保持当前动画）
    ///   - 打断（停止当前动画，播放新动画）
    ///   - 排队（等待当前动画完成后再播放）
    ///
    /// 建议在实现中区分失败原因并记录日志，便于调试。
    /// </remarks>
    bool PlayOneShot(string animationName, float transitionTime = 0.1f);

    /// <summary>是否正在播放 OneShot 动画</summary>
    bool IsPlayingOneShot { get; }

    /// <summary>OneShot 动画完成时触发的事件</summary>
    /// <remarks>
    /// 事件参数为动画名称（animationName），用于区分不同的 OneShot 动画
    /// 当有多个 OneShot（Roll、Vault、Melee1）并发请求时，Controller 可以通过参数识别是哪个动画完成
    /// </remarks>
    event Action<string> OnOneShotFinished;

    // ========== 动画参数设置 ==========

    /// <summary>设置动画参数</summary>
    void SetFloat(string parameterName, float value);
    void SetBool(string parameterName, bool value);
    void SetInt(string parameterName, int value);

    /// <summary>获取 RootMotion 增量（当策略为 RootMotionAuthority 时）</summary>
    RootMotionData GetRootMotionDelta();
}
```

#### `RootMotionData`
RootMotion 数据。

```csharp
/// <summary>
/// RootMotion 数据
/// </summary>
public struct RootMotionData
{
    public Vector3 DeltaPosition;
    public Quaternion DeltaRotation;
    public bool HasPosition;
    public bool HasRotation;
}
```

---

### 3.6 模式管理接口

#### `IPlayerMode`
玩家模式接口，定义不同模式的行为规则。

```csharp
/// <summary>
/// 玩家模式接口 - 定义模式特定的行为规则
/// </summary>
public interface IPlayerMode
{
    /// <summary>模式类型</summary>
    PlayerMode ModeType { get; }

    /// <summary>获取运动策略</summary>
    MotionPolicy GetMotionPolicy();

    /// <summary>获取最大速度</summary>
    float GetMaxSpeed();

    /// <summary>获取转向速度</summary>
    float GetTurnSpeed();

    /// <summary>获取相机参数（包含插值策略 BlendMode 和 BlendDuration）</summary>
    CameraParams GetCameraParams();

    /// <summary>是否可以移动</summary>
    bool CanMove();

    /// <summary>是否可以旋转</summary>
    bool CanRotate();

    /// <summary>是否可以跳跃</summary>
    bool CanJump();

    /// <summary>进入模式时调用</summary>
    /// <remarks>
    /// 在此方法中：
    /// - 订阅必要的事件（如 AnimationDriver.OnOneShotFinished）
    /// - 初始化模式特定状态
    /// </remarks>
    void OnEnter();

    /// <summary>退出模式时调用</summary>
    /// <remarks>
    /// ⚠️ 必须在此方法中：
    /// - 取消订阅所有事件（防止事件泄漏）
    /// - 清理模式特定资源
    /// </remarks>
    void OnExit();

    /// <summary>更新模式（每帧调用）</summary>
    void Update(float deltaTime);
}
```

---

## 四、核心类设计

### 4.1 输入层实现类

#### `NewInputSystemSource`
基于 Unity New Input System 的输入源实现。

**Pressed vs Held 实现**：
- `JumpPressed`：使用 `InputAction.WasPressedThisFrame()` 检测按下沿
- `SprintHeld`：使用 `InputAction.IsPressed()` 检测持续按住
- 避免使用 `InputAction.triggered`（语义不明确）

**注意**：`Consumed` 字段由 Controller 内部管理，InputSource 不需要初始化（默认为 `false` 即可）

```csharp
/// <summary>
/// New Input System 输入源实现
/// 支持键盘鼠标和手柄
///
/// 实现要点：
/// - JumpPressed: InputAction.WasPressedThisFrame()
/// - SprintHeld: InputAction.IsPressed()
/// - Consumed 字段由 Controller 内部管理，无需初始化
/// </summary>
public class NewInputSystemSource : MonoBehaviour, IInputSource
{
    // 依赖：Unity Input System
    // 职责：将 InputAction 转换为 PlayerCommand
    // 关键：正确区分 Pressed（按下沿）和 Held（持续按住）
    // 注意：Consumed 字段由 Controller 内部使用，InputSource 不需要关心
}
```

#### `TouchInputSource`
触控输入源实现。

```csharp
/// <summary>
/// 触控输入源实现
/// 支持手机触控，无 UGUI 依赖
/// </summary>
public class TouchInputSource : MonoBehaviour, IInputSource
{
    // 职责：将触控手势转换为 PlayerCommand
}
```

#### `CompositeInputSource`
复合输入源，自动选择当前活跃设备。

```csharp
/// <summary>
/// 复合输入源
/// 自动检测并切换到当前活跃的输入设备
/// </summary>
public class CompositeInputSource : IInputSource
{
    // 职责：管理多个输入源，选择优先级最高的
}
```

---

### 4.2 控制层实现类

#### `ThirdPersonControllerCore`
第三人称控制器核心实现（纯 C# 类）。

```csharp
/// <summary>
/// 第三人称控制器核心
/// 规则中枢，负责编排和决策
/// 纯 C# 类，无 Unity 依赖，便于单元测试和 Server-side 模拟
/// </summary>
public class ThirdPersonControllerCore : IPlayerController
{
    // 依赖：IInputSource, ICharacterMotor, ICameraRig, IAnimationDriver
    // 职责：
    // 1. 在 Tick() 内部统一读取 PlayerCommand（每帧获取命令副本，局部变量）
    // 2. 决定当前 PlayerMode
    // 3. 决定 MotionPolicy
    // 4. 分发指令给 Motor / Camera / AnimationDriver
    // 5. 在内部标记 Consumed（局部变量），通过事件对外暴露已消耗的动作
    //
    // ⚠️ 输入读取策略：ControllerCore.Tick() 内部统一读取输入
    // 外部不应在 Tick() 之前调用 ReadCommand()，避免输入不一致（鼠标 delta/触控 delta）

    // 模式注册表
    private Dictionary<PlayerMode, IPlayerMode> modeRegistry = new Dictionary<PlayerMode, IPlayerMode>();

    // 注册模式
    public void RegisterMode(IPlayerMode mode)
    {
        if (mode == null)
            throw new ArgumentNullException(nameof(mode));
        modeRegistry[mode.ModeType] = mode;
    }

    // 获取模式实例
    private IPlayerMode GetModeInstance(PlayerMode mode)
    {
        if (!modeRegistry.TryGetValue(mode, out var instance))
        {
            Debug.LogError($"[Player3C] Mode {mode} not registered");
            return null;
        }
        return instance;
    }

    // Setter 注入方法（实现 IPlayerController 接口）
    public void SetInputSource(IInputSource inputSource) { ... }
    public void SetMotor(ICharacterMotor motor) { ... }
    public void SetCameraRig(ICameraRig cameraRig) { ... }
    public void SetAnimationDriver(IAnimationDriver animationDriver) { ... }

    // 对外暴露的事件（用于通知其他系统已消耗的动作）
    public event Action OnJumpConsumed;
    public event Action OnInteractConsumed;
}
```

#### `ThirdPersonControllerMono`
Unity 适配层，负责生命周期管理和组件装配。

```csharp
/// <summary>
/// 第三人称控制器 Unity 适配层
/// 负责 MonoBehaviour 生命周期管理和组件引用装配
/// </summary>
public class ThirdPersonControllerMono : MonoBehaviour
{
    private ThirdPersonControllerCore core;

    // 依赖：IInputSource, ICharacterMotor, ICameraRig, IAnimationDriver（通过 GetComponent 获取）
    // 职责：
    // 1. 在 Awake/Start 中装配依赖
    // 2. 创建 ThirdPersonControllerCore 实例
    // 3. 在 Update 中调用 core.Tick()
}
```

#### `LocomotionMode`
正常移动模式实现。

```csharp
/// <summary>
/// 正常移动模式
/// </summary>
public class LocomotionMode : IPlayerMode
{
    // 实现正常移动的行为规则
    // GetCameraParams() 返回默认相机参数，BlendMode = Smooth
}
```

#### `AimMode`
瞄准模式实现。

```csharp
/// <summary>
/// 瞄准模式
/// </summary>
public class AimMode : IPlayerMode
{
    // 实现瞄准模式的行为规则
    // GetCameraParams() 返回瞄准相机参数：
    // - FOV 更小（拉近视角）
    // - BlendMode = EaseInOut（平滑过渡）
    // - BlendDuration = 0.3f（0.3秒过渡时间）
}
```

---

### 4.3 运动层实现类

#### `CharacterControllerMotor`
基于 CharacterController 的运动实现。

```csharp
/// <summary>
/// CharacterController 运动实现
/// </summary>
public class CharacterControllerMotor : MonoBehaviour, ICharacterMotor
{
    // 依赖：CharacterController
    // 职责：通过 CharacterController 执行位移和碰撞
}
```

#### `RigidbodyMotor`
基于 Rigidbody 的运动实现。

```csharp
/// <summary>
/// Rigidbody 运动实现
/// </summary>
public class RigidbodyMotor : MonoBehaviour, ICharacterMotor
{
    // 依赖：Rigidbody
    // 职责：通过 Rigidbody 执行物理运动
}
```

---

### 4.4 相机层实现类

#### `CustomCameraRig`
自定义相机实现。

```csharp
/// <summary>
/// 自定义相机实现
///
/// 插值策略实现要点：
/// - SetCameraParams() 时，根据 BlendMode 决定插值方式
/// - Instant：立即设置目标参数
/// - Smooth：使用 Mathf.Lerp 线性插值
/// - EaseInOut：使用缓入缓出曲线插值（如 Mathf.SmoothStep）
/// - 在 Tick() 中每帧更新插值进度，直到完成
/// </summary>
public class CustomCameraRig : MonoBehaviour, ICameraRig
{
    // 职责：实现第三人称相机跟随和视角控制
    // 关键：正确处理相机参数的插值过渡
}
```

#### `CinemachineCameraRig`
Cinemachine 相机实现。

```csharp
/// <summary>
/// Cinemachine 相机实现
///
/// 插值策略实现要点：
/// - 利用 Cinemachine 的 Blend 功能实现插值
/// - 根据 BlendMode 选择对应的 Cinemachine Blend 类型
/// - Instant → CinemachineBlendDefinition.Style.Cut
/// - Smooth → CinemachineBlendDefinition.Style.EaseInOut
/// - EaseInOut → CinemachineBlendDefinition.Style.EaseInOut（带自定义曲线）
/// </summary>
public class CinemachineCameraRig : MonoBehaviour, ICameraRig
{
    // 依赖：Cinemachine
    // 职责：封装 Cinemachine 为 ICameraRig 接口
    // 关键：利用 Cinemachine 的 Blend 功能实现插值策略
}
```

---

### 4.5 动画层实现类

#### `AnimatorDriver`
基于 Animator 的动画驱动实现。

```csharp
/// <summary>
/// Animator 动画驱动实现
///
/// OneShot 实现要点：
/// - 在 Tick() 中检测当前播放的动画状态
/// - 使用 AnimatorStateInfo.normalizedTime >= 1.0f 判断动画完成
/// - 动画完成时设置 IsPlayingOneShot = false 并触发 OnOneShotFinished(animationName)
/// - 事件必须携带 animationName 参数，用于区分不同的 OneShot 动画
///
/// 并发仲裁规则（实现时需确定）：
/// - 当 Roll 正在播放时，Vault 请求来了，可以选择：
///   - 拒绝（返回 false，保持当前动画，记录日志："OneShot busy: Roll, requested: Vault"）
///   - 打断（停止当前动画，播放新动画，记录日志："OneShot interrupted: Roll -> Vault"）
///   - 排队（等待当前动画完成后再播放，记录日志："OneShot queued: Vault (waiting for Roll)"）
///
/// 失败原因日志区分：
/// - 动画不存在：Debug.LogWarning($"[AnimatorDriver] Animation not found: {animationName}")
/// - 当前忙：Debug.Log($"[AnimatorDriver] OneShot busy: {currentAnimation}, requested: {animationName}")
/// </summary>
public class AnimatorDriver : MonoBehaviour, IAnimationDriver
{
    // 依赖：Animator
    // 职责：通过 Animator 控制动画播放
    // 关键：正确检测 OneShot 动画完成状态
    // 关键：实现并发仲裁规则（拒绝/打断/排队）
    // 关键：区分失败原因并记录日志
}
```

#### `PlayablesDriver`
基于 Playables 的动画驱动实现。

```csharp
/// <summary>
/// Playables 动画驱动实现
///
/// OneShot 实现要点：
/// - 在 Tick() 中检测 PlayableGraph 的播放状态
/// - 使用 Playable.GetTime() 和 Playable.GetDuration() 判断动画完成
/// - 动画完成时设置 IsPlayingOneShot = false 并触发 OnOneShotFinished(animationName)
/// - 事件必须携带 animationName 参数，用于区分不同的 OneShot 动画
///
/// 并发仲裁规则（实现时需确定）：
/// - 当 Roll 正在播放时，Vault 请求来了，可以选择：
///   - 拒绝（返回 false，保持当前动画，记录日志："OneShot busy: Roll, requested: Vault"）
///   - 打断（停止当前动画，播放新动画，记录日志："OneShot interrupted: Roll -> Vault"）
///   - 排队（等待当前动画完成后再播放，记录日志："OneShot queued: Vault (waiting for Roll)"）
///
/// 失败原因日志区分：
/// - 动画不存在：Debug.LogWarning($"[PlayablesDriver] Animation not found: {animationName}")
/// - 当前忙：Debug.Log($"[PlayablesDriver] OneShot busy: {currentAnimation}, requested: {animationName}")
/// </summary>
public class PlayablesDriver : MonoBehaviour, IAnimationDriver
{
    // 依赖：PlayableGraph
    // 职责：通过 PlayableGraph 控制动画播放
    // 关键：正确检测 OneShot 动画完成状态
    // 关键：实现并发仲裁规则（拒绝/打断/排队）
    // 关键：区分失败原因并记录日志
}
```

---

## 五、帧契约（Frame Contract）

### 5.1 更新顺序规范

**⚠️ 关键约束**：更新顺序直接影响游戏手感和正确性。错误的顺序会导致"输入延迟一帧"等典型问题。

**标准更新顺序**：

```
Unity 生命周期阶段：
├─ Update Early
│  └─ ThirdPersonControllerCore.Tick() ← 处理输入，分发指令
│     ├─ 内部调用：inputSource.ReadCommand() ← Controller 内部统一读取输入
│     ├─ Motor.SetMoveIntent()        ← 【必须】在 Motor.Tick() 前调用
│     ├─ Motor.SetRotationIntent()    ← 【必须】在 Motor.Tick() 前调用
│     ├─ Camera.SetLookInput()
│     └─ AnimationDriver.SetMotorState()
│
├─ Update Mid
│  └─ ICharacterMotor.Tick()          ← 执行运动计算（使用已设置的 Intent）
│
├─ Update Late / LateUpdate
│  ├─ ICameraRig.Tick()                ← 更新相机（通常 LateUpdate 更合适）
│  └─ IAnimationDriver.Tick()          ← 更新动画（取决于动画图）
```

**时序约束**：

1. **Controller → Motor**（严格顺序）
   - `Controller.Tick()` 内部会调用 `InputSource.ReadCommand()`（统一读取输入）
   - `Controller.Tick()` 必须在 `Motor.Tick()` 之前调用
   - **Motor 的 Intent 必须在 Tick 前写入**：`SetMoveIntent()` / `SetRotationIntent()` 必须在 `Motor.Tick()` 之前调用
   - **⚠️ 禁止外部提前读取输入**：外部不应在 `Controller.Tick()` 之前调用 `ReadCommand()`，否则会导致输入不一致（尤其鼠标 delta/触控 delta）

2. **Motor → Camera → Animation**（依赖顺序）
   - `Motor.Tick()` 必须在 `Camera.Tick()` 之前（Camera 可能需要 Motor 的状态）
   - `Camera.Tick()` 和 `AnimationDriver.Tick()` 的顺序取决于具体需求

**常见问题**：
- ❌ **错误**：先调用 `Motor.Tick()` 再调用 `SetMoveIntent()` → 导致输入延迟一帧
- ❌ **错误**：在 `LateUpdate` 中调用 `Controller.Tick()` → 输入响应延迟
- ❌ **错误**：`Camera.Tick()` 在 `Motor.Tick()` 之前 → 相机可能使用过时的位置信息
- ❌ **错误**：外部在 `Controller.Tick()` 之前调用 `ReadCommand()` → 导致输入不一致（鼠标 delta/触控 delta 被读取两次）

**实现建议**：
- 使用 Unity 的 `ScriptExecutionOrder` 属性明确执行顺序
- 或在统一的更新管理器（如 `Player3CManager`）中按顺序调用
- 在代码注释中明确标注每个 `Tick()` 方法的调用时机
- **⚠️ ControllerCore.Tick() 内部统一读取输入，外部不应提前读取**

**示例实现**：
```csharp
public class Player3CManager : MonoBehaviour
{
    private IPlayerController controller;
    private ICharacterMotor motor;
    private ICameraRig camera;
    private IAnimationDriver animation;

    void Update()
    {
        // 1. Controller 处理并分发（Update Early-Mid）
        // Controller.Tick() 内部会调用 inputSource.ReadCommand() 统一读取输入
        controller.Tick(Time.deltaTime);
        // Controller 内部会：
        // - 调用 inputSource.ReadCommand() 读取输入
        // - 调用 motor.SetMoveIntent(...)
        // - 调用 motor.SetRotationIntent(...)

        // 2. Motor 执行运动（Update Mid）
        motor.Tick(Time.deltaTime);

        // 3. Camera 更新（LateUpdate）
        camera.Tick(Time.deltaTime);

        // 4. Animation 更新（Update Late）
        animation.Tick(Time.deltaTime);
    }
}
```

---

## 六、数据流设计

### 6.1 正常移动流程

```
1. ThirdPersonControllerCore.Tick()
   ├─ 内部调用：inputSource.ReadCommand() ← Controller 内部统一读取输入
   ├─ 读取 PlayerCommand（获取命令副本，局部变量）
   ├─ 根据模式决定规则
   ├─ 分发指令：
   │  ├─ Motor.SetMoveIntent()
   │  ├─ Motor.SetRotationIntent()
   │  ├─ Camera.SetLookInput()
   │  └─ AnimationDriver.SetMotorState()
   ↓
2. Motor.Tick()
   ├─ 执行位移计算
   ├─ 处理碰撞
   └─ 更新 MotorState
   ↓
3. Camera.Tick()
   ├─ 处理视角输入
   └─ 更新相机位置
   ↓
4. AnimationDriver.Tick()
   ├─ 读取 MotorState
   ├─ 更新动画参数
   └─ 播放动画
```

### 6.2 跳跃流程（职责分离 + 消费语义示例）

```
1. ThirdPersonControllerCore.Tick()
   ├─ 内部调用：var cmd = inputSource.ReadCommand()（获取命令副本，局部变量）
   ├─ 检查：cmd.JumpPressed（局部变量，无需检查 Consumed）
   ├─ 【规则判断】检查：IsGrounded && CanJump() && CurrentMode 允许跳跃
   ├─ 如果通过：
   │  ├─ 调用 Motor.Jump(jumpForce)
   │  ├─ cmd.JumpConsumed = true（内部标记消费，仅对局部变量有效）
   │  └─ 触发事件：OnJumpConsumed?.Invoke()（对外暴露已消耗的动作）
   └─ Motor 不做判断，直接执行跳跃物理计算
   ↓
2. Motor.Jump()
   ├─ 应用跳跃力
   ├─ 更新速度
   └─ 更新 MotorState
   ↓
3. 其他系统（如技能系统、载具系统）订阅事件
   ├─ 订阅：Controller.OnJumpConsumed += HandleJump
   ├─ 如果 Controller 已消费跳跃，事件会通知其他系统
   └─ 避免多系统重复处理同一输入
```

**关键点**：
- Controller 负责规则判断（`if (IsGrounded && CanJump)`）
- Motor 只负责执行（应用力、更新速度）
- Motor 可以做物理安全夹紧（`jumpForce = Max(jumpForce, 0)`），但不做业务规则判断
- 禁止在 Motor.Jump() 内部再次判断 `if (IsGrounded)`（业务规则判断）
- **使用 `JumpPressed`（按下沿）而非 `JumpHeld`，避免连跳 bug**
- 详细说明参见 13.1 Motor 职责边界章节
- **消费语义**：
  - Controller 内部使用局部变量 `cmd` 消费（`cmd.JumpConsumed = true`）
  - 通过事件 `OnJumpConsumed` 对外暴露已消耗的动作
  - **禁止**：依赖 struct 的可变副本实现跨系统消费共享

### 6.3 OneShot 动画流程（控制权返回示例）

```
1. Controller 触发 OneShot 动画（如翻滚、攀爬）
   ├─ bool success = AnimationDriver.PlayOneShot("Roll", 0.1f)
   ├─ if (success) { 切换到临时模式（如 RollMode） }
   ├─ 订阅事件：AnimationDriver.OnOneShotFinished += OnRollFinished
   └─ AnimationDriver.IsPlayingOneShot == true
   ↓
2. 动画播放期间
   ├─ Controller 检查：if (AnimationDriver.IsPlayingOneShot) { 保持当前模式 }
   └─ 禁止在动画期间切换回正常模式
   ↓
3. 动画完成
   ├─ AnimationDriver 检测到动画结束
   ├─ 触发 OnOneShotFinished("Roll") 事件（携带动画名称）
   ├─ AnimationDriver.IsPlayingOneShot == false
   └─ Controller 收到事件，通过 animationName 参数识别是哪个动画完成
   ↓
4. Controller 安全切换回正常模式
   ├─ 取消订阅事件：AnimationDriver.OnOneShotFinished -= OnRollFinished
   ├─ SetMode(PlayerMode.Locomotion)
   └─ 无需依赖"魔法时间"（硬编码时间值）
```

**关键点**：
- **使用 `IsPlayingOneShot` 查询状态**：Controller 可以每帧检查，安全地等待动画完成
- **使用 `OnOneShotFinished(string animationName)` 事件**：事件携带动画名称，用于区分不同的 OneShot 动画
- **事件参数识别**：当有多个 OneShot（Roll、Vault、Melee1）并发请求时，通过 `animationName` 参数识别是哪个动画完成
- **避免"魔法时间"**：不再需要硬编码 `yield return new WaitForSeconds(1.5f)` 这样的时间值
- **精确控制**：动画实际播放时长可能因速度、混合等因素变化，使用状态查询/事件更可靠
- **⚠️ 必须取消订阅**：Controller 在 Enter/Exit 模式时必须解除订阅，避免事件泄漏或重复订阅

**实现示例**：
```csharp
// 方式 1：状态查询（每帧检查）
if (animationDriver.IsPlayingOneShot)
{
    // 保持当前模式，等待动画完成
    return;
}
SetMode(PlayerMode.Locomotion);

// 方式 2：事件订阅（推荐，支持多 OneShot 并发）
private void OnRollFinished(string animationName)
{
    if (animationName == "Roll")
    {
        // 翻滚动画完成，切换回正常模式
        animationDriver.OnOneShotFinished -= OnRollFinished; // 取消订阅
        SetMode(PlayerMode.Locomotion);
    }
}

// 在模式进入时订阅
void OnEnter()
{
    animationDriver.OnOneShotFinished += OnRollFinished;
    bool success = animationDriver.PlayOneShot("Roll", 0.1f);
    if (!success)
    {
        Debug.LogWarning("[RollMode] Failed to play Roll animation");
        // 如果动画播放失败，可能需要回退到正常模式
    }
}

// 在模式退出时取消订阅（必须！）
void OnExit()
{
    animationDriver.OnOneShotFinished -= OnRollFinished; // 防止事件泄漏
}
```

### 6.4 RootMotion 流程（详细实现）

**完整流程**：

```
1. Controller.Tick()
   ├─ 检查 CurrentMotionPolicy
   ├─ 如果是 RootMotionAuthority 或 Hybrid 或 MotorWithRootRotation：
   │  └─ 调用 AnimationDriver.GetRootMotionDelta()
   ↓
2. AnimationDriver.GetRootMotionDelta()
   ├─ 从 Animator/Playables 读取 RootMotion 增量
   ├─ 返回 RootMotionData（包含 DeltaPosition 和 DeltaRotation）
   └─ 如果当前没有 RootMotion，返回 HasPosition = false / HasRotation = false
   ↓
3. Motor.ApplyRootMotion(deltaPosition, deltaRotation)
   ├─ 接收 RootMotion 增量
   ├─ 根据 MotionPolicy 处理：
   │  ├─ RootMotionAuthority：直接应用位移（Motor 只处理碰撞）
   │  ├─ Hybrid：RootMotion 提供基础位移，Motor 处理碰撞和约束
   │  ├─ MotorWithRootRotation：只应用旋转，位移由 Motor 控制
   │  └─ MotorAuthority：不使用 RootMotion（此方法不应被调用）
   ├─ 应用碰撞约束（防止穿墙）
   └─ 更新位置和旋转
```

**关键点**：
- **RootMotion 的读取时机**：在 `Controller.Tick()` 中，根据 `MotionPolicy` 决定是否读取
- **Motor 的处理策略**：根据不同的 `MotionPolicy` 采用不同的处理方式
  - `RootMotionAuthority`：RootMotion 完全控制位移，Motor 只处理碰撞
  - `Hybrid`：RootMotion 提供基础位移，Motor 在此基础上处理碰撞和约束
  - `MotorWithRootRotation`：RootMotion 只控制旋转，位移由 Motor 控制
- **碰撞约束**：即使使用 RootMotion，Motor 仍需要处理碰撞，防止穿墙
- **实现示例**：
  ```csharp
  // Controller.Tick() 中
  void Tick(float deltaTime)
  {
      var cmd = inputSource.ReadCommand();

      // 根据 MotionPolicy 决定是否读取 RootMotion
      if (CurrentMotionPolicy == MotionPolicy.RootMotionAuthority ||
          CurrentMotionPolicy == MotionPolicy.Hybrid ||
          CurrentMotionPolicy == MotionPolicy.MotorWithRootRotation)
      {
          var rootMotion = animationDriver.GetRootMotionDelta();

          if (rootMotion.HasPosition || rootMotion.HasRotation)
          {
              motor.ApplyRootMotion(
                  rootMotion.DeltaPosition,
                  rootMotion.DeltaRotation
              );
          }
      }

      // ... 其他逻辑
  }

  // Motor.ApplyRootMotion() 中
  void ApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
  {
      switch (CurrentPolicy)
      {
          case MotionPolicy.RootMotionAuthority:
              // RootMotion 完全控制位移
              transform.position += deltaPosition;
              transform.rotation *= deltaRotation;
              // 只处理碰撞约束
              ResolveCollisions();
              break;

          case MotionPolicy.Hybrid:
              // RootMotion 提供基础位移，Motor 处理碰撞
              var desiredPosition = transform.position + deltaPosition;
              MoveWithCollision(desiredPosition);
              transform.rotation *= deltaRotation;
              break;

          case MotionPolicy.MotorWithRootRotation:
              // 只应用旋转，位移由 Motor 控制
              transform.rotation *= deltaRotation;
              // 位移在 SetMoveIntent() + Tick() 中处理
              break;
      }
  }
  ```

### 6.5 模式切换流程（含相机插值 + 事件管理）

```
1. Controller.SetMode(newMode)
   ↓
2. 当前模式 OnExit()
   ├─ 取消订阅所有事件（防止事件泄漏）
   │  ├─ AnimationDriver.OnOneShotFinished -= handler
   │  └─ 其他事件订阅...
   └─ 清理模式特定资源
   ↓
3. 新模式 OnEnter()
   ├─ 订阅必要的事件
   │  ├─ AnimationDriver.OnOneShotFinished += OnOneShotFinished
   │  └─ 其他事件订阅...
   └─ 初始化模式特定状态
   ↓
4. Controller 更新规则
   ├─ 更新 MotionPolicy
   ├─ 更新速度限制
   └─ 获取新模式的 CameraParams（包含 BlendMode 和 BlendDuration）
   ↓
5. Camera.SetCameraParams(newParams)
   ├─ 根据 BlendMode 决定插值策略：
   │  ├─ Instant：立即切换，无插值
   │  ├─ Smooth：线性插值，持续 BlendDuration 秒
   │  └─ EaseInOut：缓入缓出插值，持续 BlendDuration 秒
   └─ 在插值期间，Camera.Tick() 每帧更新参数
```

**关键点**：
- **不同模式使用不同的插值策略**：
  - Locomotion → Aim：使用 `EaseInOut`，平滑过渡到瞄准视角
  - Locomotion → LockOn：使用 `Smooth`，快速切换到锁定视角
  - 任何模式 → Vehicle：使用 `Instant`，立即切换到载具视角
- **插值持续时间可配置**：每个模式可以设置不同的 `BlendDuration`
- **Camera 负责插值计算**：Controller 只需设置参数，Camera 内部处理插值逻辑

### 6.6 模式更新流程

**IPlayerMode.Update() 的调用时机**：

```
1. Controller.Tick()
   ├─ 读取输入
   ├─ 调用 CurrentMode.Update(deltaTime) ← 模式特定逻辑更新
   │  ├─ AimMode.Update()：更新瞄准逻辑、目标跟踪、准星位置
   │  ├─ LockOnMode.Update()：更新锁定目标、目标切换、相机跟随逻辑
   │  ├─ ClimbingMode.Update()：更新攀爬状态机、抓取点检测、攀爬路径计算
   │  └─ SwimmingMode.Update()：更新游泳状态、浮力计算、水面检测
   ├─ 根据模式规则处理输入
   ├─ 分发指令给 Motor / Camera / AnimationDriver
   └─ ...
```

**关键点**：
- **调用时机**：`IPlayerMode.Update()` 在 `Controller.Tick()` 中调用，每帧执行
- **用途**：用于模式特定的逻辑更新
  - `AimMode`：瞄准逻辑、目标跟踪、准星位置计算
  - `LockOnMode`：锁定目标更新、目标切换、相机跟随逻辑
  - `ClimbingMode`：攀爬状态机、抓取点检测、攀爬路径计算
  - `SwimmingMode`：游泳状态、浮力计算、水面检测
- **简单模式**：不是所有模式都需要复杂逻辑，简单模式（如 `LocomotionMode`）可以留空或只做基础检查
- **实现示例**：
  ```csharp
  // Controller.Tick() 中
  void Tick(float deltaTime)
  {
      var cmd = inputSource.ReadCommand();

      // 调用模式的 Update 方法（模式特定逻辑）
      if (currentMode != null)
      {
          currentMode.Update(deltaTime);
      }

      // 根据模式规则处理输入
      if (currentMode.CanMove())
      {
          motor.SetMoveIntent(CalculateMoveDirection(cmd), CalculateSpeed(cmd));
      }

      // ... 其他逻辑
  }

  // AimMode.Update() 示例
  void Update(float deltaTime)
  {
      // 更新瞄准逻辑
      UpdateAimTarget();

      // 更新准星位置
      UpdateCrosshair();

      // 检查目标是否在范围内
      ValidateTargetRange();
  }

  // LocomotionMode.Update() 示例（简单模式，可以留空）
  void Update(float deltaTime)
  {
      // 简单模式不需要复杂逻辑，可以留空
      // 或者只做一些基础检查
  }
  ```

---

## 七、扩展点设计

### 7.1 输入扩展

**添加新输入设备**：
1. 实现 `IInputSource` 接口
2. 在 `CompositeInputSource` 中注册
3. 无需修改其他系统

### 7.2 运动扩展

**切换运动实现**：
1. 实现 `ICharacterMotor` 接口
2. 在 Controller 中替换引用
3. 无需修改其他系统

### 7.3 相机扩展

**切换相机实现**：
1. 实现 `ICameraRig` 接口
2. 在 Controller 中替换引用
3. 无需修改其他系统

### 7.4 动画扩展

**切换动画实现**：
1. 实现 `IAnimationDriver` 接口
2. 在 Controller 中替换引用
3. 无需修改其他系统

### 7.5 模式扩展

**添加新模式**：
1. 实现 `IPlayerMode` 接口
2. 在 Controller 中注册
3. 通过 `SetMode()` 切换

---

## 八、生命周期管理

### 8.1 MonoBehaviour 职责

MonoBehaviour 只负责：
- 组件引用管理
- Awake/Start 装配
- Update 驱动 Tick（**必须遵循 Frame Contract 的更新顺序**）

**⚠️ 重要**：Update 中的调用顺序必须严格遵循 Frame Contract（帧契约），否则会导致输入延迟一帧等问题。

1. 实现时必须二选一，否则会出现相机/动画每帧 Tick 两次的隐蔽 bug（抖动、插值超速、状态机异常）。
2. 建议你落地时定一个唯一执行策略，例如：
3. Update：Controller + Motor
4. LateUpdate：Camera + Animation

### 8.2 核心系统初始化

```csharp
public class ThirdPersonControllerMono : MonoBehaviour
{
    private ThirdPersonControllerCore core;
    private IInputSource inputSource;
    private ICharacterMotor motor;
    private ICameraRig cameraRig;
    private IAnimationDriver animationDriver;

    void Awake()
    {
        // 装配依赖
        inputSource = GetComponent<IInputSource>();
        motor = GetComponent<ICharacterMotor>();
        cameraRig = GetComponent<ICameraRig>();
        animationDriver = GetComponent<IAnimationDriver>();

        // 创建控制器核心（纯 C# 类）
        core = new ThirdPersonControllerCore();
        core.SetInputSource(inputSource);
        core.SetMotor(motor);
        core.SetCameraRig(cameraRig);
        core.SetAnimationDriver(animationDriver);

        // 注册模式
        core.RegisterMode(new LocomotionMode());
        core.RegisterMode(new AimMode());
        core.RegisterMode(new LockOnMode());
        // ... 注册其他模式

        // 设置默认模式
        core.SetMode(PlayerMode.Locomotion);
    }

    void Update()
    {
        // ⚠️ 严格遵循 Frame Contract（帧契约）的更新顺序
        // 1. Controller.Tick() 内部会：
        //    - 调用 inputSource.ReadCommand() 统一读取输入
        //    - 调用 motor.SetMoveIntent() / SetRotationIntent()
        //    - 分发指令给 Camera 和 AnimationDriver
        core.Tick(Time.deltaTime);

        // 2. Motor.Tick() 必须在 SetIntent 之后调用
        motor.Tick(Time.deltaTime);

        // 3. Camera.Tick() 在 Motor 之后（可能需要 Motor 的状态）
        cameraRig.Tick(Time.deltaTime);

        // 4. AnimationDriver.Tick() 最后更新
        animationDriver.Tick(Time.deltaTime);
    }

    // 或者使用 LateUpdate 更新 Camera 和 Animation（更推荐）
    void LateUpdate()
    {
        cameraRig.Tick(Time.deltaTime);
        animationDriver.Tick(Time.deltaTime);
    }

    // ⚠️ 禁止：不要在外部提前读取输入
    // ❌ 错误示例：
    // void Update()
    // {
    //     var cmd = inputSource.ReadCommand(); // ❌ 不要这样做！
    //     core.Tick(Time.deltaTime); // Controller 内部会再次读取，导致输入不一致
    // }
}
```

---

## 九、测试策略

### 9.1 单元测试

所有核心系统（Controller、Motor、Camera）都是纯 C# 类，便于单元测试：

```csharp
[Test]
public void TestMotorMovement()
{
    var motor = new CharacterControllerMotor();
    motor.SetMoveIntent(Vector3.forward, 5f);
    motor.Tick(0.016f);
    // 验证位移
}
```

### 9.2 集成测试

通过接口 Mock 测试系统间交互：

```csharp
[Test]
public void TestControllerIntegration()
{
    var mockInput = new Mock<IInputSource>();
    var mockMotor = new Mock<ICharacterMotor>();
    var controller = new ThirdPersonControllerCore();

    controller.SetInputSource(mockInput.Object);
    controller.Tick(0.016f);

    // 验证 mockMotor 被正确调用
}
```

---

## 十、性能考虑

### 10.1 对象分配

- `PlayerCommand` 使用 `struct` 避免堆分配
- `MotorState`、`ControllerState` 使用 `struct`
- 考虑对象池管理临时对象

### 10.2 更新频率

- Motor、Camera 每帧更新
- AnimationDriver 每帧更新
- Controller 每帧更新
- InputSource 每帧读取

---

## 十一、调试支持

### 11.1 可视化调试

建议添加调试组件：

```csharp
#if UNITY_EDITOR
public class Player3CDebugger : MonoBehaviour
{
    [SerializeField] private bool showGizmos;
    [SerializeField] private bool showVelocity;
    [SerializeField] private bool showCameraTarget;

    void OnDrawGizmos()
    {
        // 绘制移动方向、速度、相机目标等
    }
}
#endif
```

### 11.2 日志系统

关键操作添加日志，便于追踪问题：

```csharp
public static class Player3CLogger
{
    public static void LogCommand(PlayerCommand cmd) { ... }
    public static void LogModeChange(PlayerMode old, PlayerMode @new) { ... }
    public static void LogMotionPolicyChange(MotionPolicy policy) { ... }
}
```

---

## 十二、实现优先级

### 阶段一：核心框架（MVP）
1. ✅ `IInputSource` 接口 + `NewInputSystemSource` 实现
2. ✅ `PlayerCommand` 结构
3. ✅ `ICharacterMotor` 接口 + `CharacterControllerMotor` 实现
4. ✅ `ICameraRig` 接口 + `CustomCameraRig` 实现
5. ✅ `IAnimationDriver` 接口 + `AnimatorDriver` 实现
6. ✅ `ThirdPersonControllerCore` 核心实现 + `ThirdPersonControllerMono` 适配层
7. ✅ `LocomotionMode` 基础模式

### 阶段二：输入扩展
1. `TouchInputSource` 实现
2. `CompositeInputSource` 实现

### 阶段三：模式扩展
1. `AimMode` 实现
2. `LockOnMode` 实现
3. 模式切换系统完善

### 阶段四：运动策略扩展
1. `RootMotionAuthority` 支持
2. `Hybrid` 模式实现
3. `MotorWithRootRotation` 实现

### 阶段五：高级功能
1. `RigidbodyMotor` 实现
2. `CinemachineCameraRig` 实现
3. `PlayablesDriver` 实现
4. 调试工具完善

---

## 十三、注意事项

### 13.1 Motor 职责边界：物理安全夹紧 vs 业务规则判断

**⚠️ 重要边界**：Motor 应该做"物理安全夹紧"，但不做"业务规则判断"。

**问题背景**：
- Controller 决定"能不能跳"（业务规则）
- Motor 决定"怎么执行"（物理计算）
- 但实现时仍需要 Motor 做防御性检查，避免数值异常

**正确的边界划分**：

- ✅ **允许 Motor 做物理安全夹紧**：
  ```csharp
  // Motor.Jump() 内部
  public void Jump(float jumpForce)
  {
      // ✅ 允许：物理安全夹紧（防止负值、NaN、Infinity）
      jumpForce = Mathf.Max(jumpForce, 0f); // 防止负值
      jumpForce = float.IsNaN(jumpForce) ? 0f : jumpForce; // 防止 NaN
      jumpForce = float.IsInfinity(jumpForce) ? maxJumpForce : jumpForce; // 防止 Infinity

      // ✅ 允许：速度上限保护（防止速度爆掉）
      velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);

      // ✅ 允许：数值范围限制
      speed = Mathf.Clamp(speed, 0f, maxSpeed);

      // 执行跳跃物理计算
      ApplyJumpForce(jumpForce);
  }
  ```

- ❌ **禁止 Motor 做业务规则判断**：
  ```csharp
  // Motor.Jump() 内部
  public void Jump(float jumpForce)
  {
      // ❌ 禁止：业务规则判断（Controller 已经判断过了）
      if (!IsGrounded) return; // 不应该再次判断
      if (!CanJump()) return; // 不应该再次判断
      if (CurrentMode != PlayerMode.Locomotion) return; // 不应该判断模式

      // 执行跳跃...
  }
  ```

**两种极端情况**：

1. ❌ **Motor 完全不防御**：
   ```csharp
   // 危险：某帧 Controller 状态异常导致速度爆掉
   public void Jump(float jumpForce)
   {
       velocity.y = jumpForce; // 如果 jumpForce 是 NaN 或 Infinity，会导致游戏崩溃
   }
   ```

2. ❌ **Motor 重新写规则**：
   ```csharp
   // 错误：破坏分层原则
   public void Jump(float jumpForce)
   {
       if (!IsGrounded && !CanDoubleJump()) return; // 重新判断规则
       // 这会导致 Controller 的判断失效
   }
   ```

**正确的实现方式**：

```csharp
// Controller：业务规则判断
void Tick(float deltaTime)
{
    var cmd = inputSource.ReadCommand();

    if (cmd.JumpPressed)
    {
        // ✅ Controller 判断业务规则
        if (IsGrounded && CanJump() && CurrentMode.AllowsJump())
        {
            // Controller 已判断可以跳，直接调用 Motor
            motor.Jump(jumpForce);
        }
    }
}

// Motor：物理安全夹紧 + 物理计算
public void Jump(float jumpForce)
{
    // ✅ Motor 做物理安全夹紧（不是业务规则判断）
    jumpForce = Mathf.Max(jumpForce, 0f);
    jumpForce = Mathf.Clamp(jumpForce, 0f, maxJumpForce);

    // ✅ Motor 执行物理计算
    velocity.y = jumpForce;
    ApplyJumpForce(jumpForce);
}
```

**关键原则**：
- Motor 的防御性检查是为了**物理安全**（防止数值异常、NaN、Infinity）
- Motor 的防御性检查**不是**为了重新判断业务规则（Controller 已经判断过了）
- 如果 Controller 判断可以跳，Motor 就应该执行，即使物理上看起来不合理（由 Controller 负责保证合理性）

**常见问题**：
- ❌ **错误**：Motor 完全不防御，导致某帧状态异常时速度爆掉
- ❌ **错误**：Motor 重新判断业务规则，破坏分层原则
- ✅ **正确**：Motor 只做物理安全夹紧，不做业务规则判断

### 13.2 依赖注入

**推荐方式**：
- **Setter 注入**（当前设计）：使用 `SetInputSource()`、`SetMotor()` 等方法
  - 优点：灵活，支持运行时替换，便于 Unity 组件装配
  - 缺点：需要手动调用多个 Setter，依赖关系不够明确
- **构造函数注入**（可选）：在构造函数中注入所有依赖
  - 优点：依赖关系明确，强制初始化，符合依赖倒置原则
  - 缺点：构造函数参数过多时不够灵活，Unity 组件装配不便

**当前设计使用 Setter 注入**，便于 Unity 组件装配和运行时替换：

```csharp
// Setter 注入示例（当前设计）
public class ThirdPersonControllerCore : IPlayerController
{
    private IInputSource inputSource;
    private ICharacterMotor motor;

    public void SetInputSource(IInputSource inputSource)
    {
        if (inputSource == null)
            throw new ArgumentNullException(nameof(inputSource));
        this.inputSource = inputSource;
    }

    public void SetMotor(ICharacterMotor motor)
    {
        if (motor == null)
            throw new ArgumentNullException(nameof(motor));
        this.motor = motor;
    }
}

// 构造函数注入示例（可选方案）
public class ThirdPersonControllerCore : IPlayerController
{
    private readonly IInputSource inputSource;
    private readonly ICharacterMotor motor;

    public ThirdPersonControllerCore(
        IInputSource inputSource,
        ICharacterMotor motor,
        ICameraRig cameraRig,
        IAnimationDriver animationDriver)
    {
        this.inputSource = inputSource ?? throw new ArgumentNullException(nameof(inputSource));
        this.motor = motor ?? throw new ArgumentNullException(nameof(motor));
        // ...
    }
}
```

**实现建议**：
- 所有依赖通过构造函数或 Setter 注入，避免在类内部查找组件（如 `GetComponent<T>()`）
- 使用 Setter 注入时，应在 Setter 中进行空值检查
- 使用构造函数注入时，应在构造函数中进行空值检查并抛出 `ArgumentNullException`

### 13.3 接口隔离

每个接口只暴露必要的方法，避免接口过于庞大。

### 13.4 单一职责

每个类只负责一件事，保持高内聚。

**Motor 职责边界示例**：
- ✅ Motor：执行位移计算、处理碰撞、应用物理约束
- ✅ Motor：做物理安全夹紧（防止数值异常、NaN、Infinity）
- ❌ Motor：判断"能不能跳"、"能不能移动"（这是 Controller 的职责，业务规则判断）
- ✅ Controller：判断规则（`if (IsGrounded && CanJump)`），然后调用 `Motor.Jump()`
- ❌ Controller：直接操作 Transform 或物理组件
- 详细说明参见 13.1 Motor 职责边界章节

### 13.5 依赖方向

严格遵守单向依赖，禁止反向引用。

### 13.6 按钮语义明确性

**必须明确区分 Pressed 和 Held**，避免语义模糊导致的 bug：

- ✅ **Pressed（按下沿）**：用于一次性动作
  - `JumpPressed`：本帧按下，避免连跳 bug
  - `InteractPressed`：本帧按下，避免重复触发

- ✅ **Held（持续按住）**：用于持续状态
  - `SprintHeld`：持续按住时冲刺
  - `AimHeld`：持续按住时瞄准

**常见问题**：
- ❌ 使用模糊的 `bool Jump`，不明确是 Pressed 还是 Held
- ❌ 在 Controller 中使用 `JumpHeld` 触发跳跃（会导致连跳）
- ❌ 在 InputSource 中使用 `InputAction.triggered`（语义不明确）

**实现建议**：
- Unity Input System：`WasPressedThisFrame()` → Pressed，`IsPressed()` → Held
- 触控输入：需要手动跟踪上一帧状态，检测按下沿

### 13.7 命令消费语义

**⚠️ 重要实现约束：`PlayerCommand` 是 `struct`（值类型）**

**问题**：如果多个系统共享同一个 `PlayerCommand` 并修改 `Consumed`，由于 struct 是值类型，修改只对副本有效，其他系统拿到的还是"未消费"的版本。

**正确的实现方式**：

- ✅ **Controller 内部消费（推荐）**：
  ```csharp
  // Controller 内部
  void Tick(float deltaTime)
  {
      // ControllerCore 内部统一读取输入（每帧读取一次）
      var cmd = inputSource?.ReadCommand() ?? default; // 获取命令副本（局部变量）

      if (cmd.JumpPressed) // 无需检查 Consumed，因为这是 Controller 的局部副本
      {
          if (CanJump())
          {
              motor.Jump(jumpForce);
              cmd.JumpConsumed = true; // 内部标记（仅对局部变量有效）

              // 对外暴露已消耗的动作（通过事件）
              OnJumpConsumed?.Invoke();
          }
      }

      // ... 处理其他输入和分发指令
  }

  // ⚠️ 外部不应提前读取输入
  // ❌ 错误示例：
  // void Update()
  // {
  //     var cmd = inputSource.ReadCommand(); // ❌ 不要这样做！
  //     controller.Tick(Time.deltaTime); // Controller 内部会再次读取，导致输入不一致（鼠标 delta/触控 delta）
  // }

  // 对外暴露的事件
  public event Action OnJumpConsumed;
  public event Action OnInteractConsumed;
  ```

- ✅ **其他系统订阅事件**：
  ```csharp
  // 技能系统、载具系统等
  controller.OnJumpConsumed += () => {
      // Controller 已消费跳跃，执行其他逻辑
  };
  ```

- ✅ **若需要多系统竞争同一输入**：
  ```csharp
  // 方案 1：使用 class（引用语义）
  public class PlayerCommand { ... } // 改为 class

  // 方案 2：引入 CommandContext（集中式路由器）
  public class CommandContext
  {
      private PlayerCommand command;
      private HashSet<string> consumedActions = new();

      public bool TryConsume(string action)
      {
          if (consumedActions.Contains(action)) return false;
          consumedActions.Add(action);
          return true;
      }
  }
  ```

**常见问题**：
- ❌ **错误**：多个系统共享同一个 `PlayerCommand` struct 并修改 `Consumed`
  ```csharp
  var cmd = input.ReadCommand();
  cmd.JumpConsumed = true; // ❌ 只修改了副本，其他系统看不到
  ```
- ❌ **错误**：依赖 struct 的可变副本实现跨系统消费共享
- ❌ 多个系统同时响应 `JumpPressed`，导致重复处理

**实现约束**：
- ✅ `Consumed` 标记仅在 Controller 内部使用（局部变量）
- ✅ Controller 通过事件对外暴露已消耗的动作
- ✅ 其他系统通过订阅事件获取消费信息，而不是直接检查 `Command.Consumed`
- ❌ **禁止**：依赖 struct 的可变副本实现跨系统消费共享
- ✅ 若需要跨系统共享消费结果，必须通过事件/路由器转发

### 13.8 OneShot 动画控制权返回

**必须使用状态查询或事件，避免依赖"魔法时间"**：

- ✅ **状态查询方式**（每帧检查）：
  ```csharp
  if (animationDriver.IsPlayingOneShot)
  {
      // 保持当前模式，等待动画完成
      return;
  }
  SetMode(PlayerMode.Locomotion);
  ```

- ✅ **事件订阅方式**（推荐，支持多 OneShot 并发）：
  ```csharp
  private void OnOneShotFinished(string animationName)
  {
      if (animationName == "Roll")
      {
          // 翻滚动画完成，切换回正常模式
          animationDriver.OnOneShotFinished -= OnOneShotFinished; // 取消订阅
          SetMode(PlayerMode.Locomotion);
      }
      else if (animationName == "Vault")
      {
          // 翻越动画完成
          // ...
      }
  }

  void OnEnter()
  {
      animationDriver.OnOneShotFinished += OnOneShotFinished; // 订阅
      bool success = animationDriver.PlayOneShot("Roll", 0.1f);
      if (!success)
      {
          Debug.LogWarning("[RollMode] Failed to play Roll animation");
          // 处理播放失败的情况
      }
  }

  void OnExit()
  {
      animationDriver.OnOneShotFinished -= OnOneShotFinished; // 必须取消订阅！
  }
  ```

**常见问题**：
- ❌ 使用硬编码时间等待动画完成：`yield return new WaitForSeconds(1.5f)`（"魔法时间"）
- ❌ 动画实际播放时长可能因速度、混合等因素变化，硬编码时间不可靠
- ❌ 在动画播放期间切换模式，导致动画被中断或行为异常
- ❌ **事件泄漏**：在模式退出时未取消订阅，导致事件累积和重复触发（Unity 项目高频事故）
- ❌ **接错回调**：多个 OneShot（Roll、Vault、Melee1）并发时，无法区分是哪个动画完成

**实现建议**：
- AnimationDriver 实现时，需要在 `Tick()` 中检测 OneShot 动画是否完成
- 动画完成时，设置 `IsPlayingOneShot = false` 并触发 `OnOneShotFinished(animationName)` 事件
- **事件参数**：事件携带 `animationName` 参数，用于区分不同的 OneShot 动画
- Controller 使用状态查询或事件订阅，精确控制模式切换时机
- **⚠️ 必须取消订阅**：Controller 在 `OnEnter()` 时订阅，在 `OnExit()` 时必须取消订阅，避免事件泄漏
- 避免在动画播放期间切换模式，除非明确需要中断动画

**并发仲裁规则实现**：

当 Roll 正在播放时，Vault 请求来了，需要确定处理策略：

1. **拒绝策略**（推荐用于大多数情况）：
   ```csharp
   bool PlayOneShot(string animationName, float transitionTime = 0.1f)
   {
       if (IsPlayingOneShot)
       {
           Debug.Log($"[AnimatorDriver] OneShot busy: {currentAnimationName}, requested: {animationName}");
           return false; // 拒绝新请求，保持当前动画
       }
       // ... 播放动画
   }
   ```
   - 优点：简单、可预测
   - 缺点：可能错过某些输入

2. **打断策略**（用于高优先级动画）：
   ```csharp
   bool PlayOneShot(string animationName, float transitionTime = 0.1f)
   {
       if (IsPlayingOneShot)
       {
           Debug.Log($"[AnimatorDriver] OneShot interrupted: {currentAnimationName} -> {animationName}");
           InterruptCurrentAnimation(); // 打断当前动画
       }
       // ... 播放新动画
   }
   ```
   - 优点：响应及时
   - 缺点：可能导致动画不完整

3. **排队策略**（用于需要完整播放的场景）：
   ```csharp
   private Queue<string> animationQueue = new Queue<string>();

   bool PlayOneShot(string animationName, float transitionTime = 0.1f)
   {
       if (IsPlayingOneShot)
       {
           Debug.Log($"[AnimatorDriver] OneShot queued: {animationName} (waiting for {currentAnimationName})");
           animationQueue.Enqueue(animationName);
           return false; // 已排队，等待当前动画完成
       }
       // ... 播放动画
   }

   void OnOneShotFinished(string animationName)
   {
       // 当前动画完成，播放队列中的下一个
       if (animationQueue.Count > 0)
       {
           string nextAnimation = animationQueue.Dequeue();
           PlayOneShot(nextAnimation);
       }
   }
   ```
   - 优点：保证动画完整性
   - 缺点：可能延迟响应

**失败原因日志区分**：

建议在实现中区分失败原因并记录日志，便于调试：

```csharp
bool PlayOneShot(string animationName, float transitionTime = 0.1f)
{
    // 检查动画是否存在
    if (!HasAnimation(animationName))
    {
        Debug.LogWarning($"[AnimatorDriver] Animation not found: {animationName}");
        return false; // 失败原因：动画不存在
    }

    // 检查是否正在播放其他 OneShot
    if (IsPlayingOneShot)
    {
        Debug.Log($"[AnimatorDriver] OneShot busy: {currentAnimationName}, requested: {animationName}");

        // 实现并发仲裁规则（拒绝/打断/排队）
        switch (concurrentPolicy)
        {
            case OneShotConcurrentPolicy.Reject:
                return false; // 失败原因：当前忙
            case OneShotConcurrentPolicy.Interrupt:
                InterruptCurrentAnimation();
                break;
            case OneShotConcurrentPolicy.Queue:
                QueueAnimation(animationName);
                return false; // 失败原因：已排队
        }
    }

    // 播放动画
    PlayAnimation(animationName, transitionTime);
    return true;
}
```

**关键点**：
- 在实现中明确并发仲裁规则（拒绝/打断/排队）
- 区分失败原因并记录日志（动画不存在 vs 当前忙）
- 使用不同的日志级别（Warning 用于动画不存在，Info 用于当前忙）
- 便于调试和问题排查

### 13.9 相机插值策略

**必须为不同模式配置合适的插值策略，确保相机切换平滑自然**：

- ✅ **Instant（立即切换）**：用于需要立即响应的场景
  - 载具模式切换：`BlendMode = Instant`
  - 紧急状态切换：需要立即改变视角

- ✅ **Smooth（线性插值）**：用于常规模式切换
  - Locomotion ↔ LockOn：`BlendMode = Smooth, BlendDuration = 0.2f`
  - 快速但平滑的过渡

- ✅ **EaseInOut（缓入缓出）**：用于需要精细控制的场景
  - Locomotion ↔ Aim：`BlendMode = EaseInOut, BlendDuration = 0.3f`
  - 更自然的过渡效果

**常见问题**：
- ❌ 所有模式都使用 `Instant`，导致相机切换生硬
- ❌ 所有模式都使用 `EaseInOut`，导致响应延迟
- ❌ 不设置 `BlendDuration`，使用默认值可能导致过渡时间不合适

**实现建议**：
- 每个模式在 `GetCameraParams()` 中明确设置 `BlendMode` 和 `BlendDuration`
- Camera 实现时，在 `SetCameraParams()` 中保存目标参数和插值配置
- 在 `Tick()` 中每帧更新插值进度，直到完成
- 插值期间，如果收到新的 `SetCameraParams()` 调用，应中断当前插值并开始新的插值

**模式切换示例**：
```csharp
// AimMode.GetCameraParams()
return new CameraParams
{
    FOV = 45f,  // 瞄准时缩小视野
    Distance = 2f,
    BlendMode = CameraBlendMode.EaseInOut,
    BlendDuration = 0.3f  // 0.3秒平滑过渡
};

// VehicleMode.GetCameraParams()
return new CameraParams
{
    FOV = 60f,
    Distance = 5f,
    BlendMode = CameraBlendMode.Instant,  // 载具切换立即生效
    BlendDuration = 0f
};
```

### 13.10 更新顺序（Frame Contract）

**必须严格遵循 Frame Contract（帧契约）的更新顺序，否则会导致输入延迟一帧等严重问题**：

- ✅ **标准更新顺序**：
  ```
  Update Early:
    - Controller.Tick() → 内部调用 inputSource.ReadCommand() 统一读取输入
    - Controller.Tick() → 内部调用 Motor.SetMoveIntent() / SetRotationIntent()

  Update Mid:
    - Motor.Tick() ← 【必须】在 SetIntent 之后调用

  Update Late / LateUpdate:
    - Camera.Tick()
    - AnimationDriver.Tick()
  ```

- ✅ **关键约束**：
  - `Motor.SetMoveIntent()` / `SetRotationIntent()` **必须在** `Motor.Tick()` 之前调用
  - `Controller.Tick()` **必须在** `Motor.Tick()` 之前调用
  - **ControllerCore.Tick() 内部统一读取输入**：`inputSource.ReadCommand()` 在 `Controller.Tick()` 内部调用
  - **⚠️ 禁止外部提前读取**：外部不应在 `Controller.Tick()` 之前调用 `ReadCommand()`

**常见问题**：
- ❌ **错误**：先调用 `Motor.Tick()` 再调用 `SetMoveIntent()` → 导致输入延迟一帧
- ❌ **错误**：在 `LateUpdate` 中调用 `Controller.Tick()` → 输入响应延迟
- ❌ **错误**：`Camera.Tick()` 在 `Motor.Tick()` 之前 → 相机可能使用过时的位置信息

**实现建议**：
- 使用 Unity 的 `[ScriptExecutionOrder]` 属性明确执行顺序
- 或在统一的更新管理器（如 `Player3CManager`）中按顺序调用
- 在代码注释中明确标注每个 `Tick()` 方法的调用时机
- **⚠️ ControllerCore.Tick() 内部统一读取输入，外部不应提前读取**
- 参见 Frame Contract（帧契约）章节了解详细更新顺序

### 13.11 错误处理和边界情况

**必须处理所有边界情况，确保系统健壮性**：

- ✅ **空值检查**：
  ```csharp
  // Controller 初始化时检查依赖
  public void SetInputSource(IInputSource inputSource)
  {
      if (inputSource == null)
          throw new ArgumentNullException(nameof(inputSource));
      this.inputSource = inputSource;
  }

  // Tick() 中检查依赖有效性
  void Tick(float deltaTime)
  {
      if (inputSource == null || !inputSource.IsEnabled) return;
      if (motor == null || !motor.IsEnabled) return; // 降级处理：Motor 缺失或禁用时跳过运动

      var cmd = inputSource.ReadCommand();
      // ... 处理命令
  }
  ```

- ✅ **异常处理策略**：
  ```csharp
  // Controller.Tick() 应该捕获并记录异常
  void Tick(float deltaTime)
  {
      try
      {
          var cmd = inputSource?.ReadCommand() ?? default;
          // ... 处理命令
      }
      catch (Exception e)
      {
          Debug.LogError($"[PlayerController] Tick failed: {e.Message}");
          // 不抛出异常，避免单帧错误影响整个系统
      }
  }

  // Motor.Tick() 中的物理计算异常
  void Tick(float deltaTime)
  {
      try
      {
          // 执行物理计算
      }
      catch (Exception e)
      {
          Debug.LogError($"[Motor] Physics calculation failed: {e.Message}");
          // 记录但不中断游戏，使用安全默认值
      }
  }

  // InputSource.ReadCommand() 异常处理
  PlayerCommand ReadCommand()
  {
      try
      {
          // 读取输入
      }
      catch (Exception e)
      {
          Debug.LogError($"[InputSource] ReadCommand failed: {e.Message}");
          return default(PlayerCommand); // 返回默认的空命令
      }
  }
  ```

- ✅ **依赖缺失时的降级处理**：
  ```csharp
  // Camera 缺失时的降级处理
  void Tick(float deltaTime)
  {
      // ... 处理输入和运动

      // Camera 缺失时，Controller 继续工作（只是没有相机响应）
      if (cameraRig != null && cameraRig.IsEnabled)
      {
          cameraRig.SetLookInput(cmd.Look);
      }

      // AnimationDriver 缺失时，Motor 继续工作（只是没有动画表现）
      if (animationDriver != null && animationDriver.IsEnabled)
      {
          animationDriver.SetMotorState(motor.GetState());
      }
  }

  // InputSource 缺失时使用默认空输入
  PlayerCommand ReadCommand()
  {
      if (inputSource == null || !inputSource.IsEnabled)
      {
          return default(PlayerCommand); // 返回空命令
      }
      return inputSource.ReadCommand();
  }
  ```

- ✅ **初始化失败处理**：
  ```csharp
  void Awake()
  {
      try
      {
          inputSource = GetComponent<IInputSource>();
          motor = GetComponent<ICharacterMotor>();
          cameraRig = GetComponent<ICameraRig>();
          animationDriver = GetComponent<IAnimationDriver>();

          if (inputSource == null)
          {
              Debug.LogWarning("[Player3C] InputSource not found, using default");
              inputSource = new DefaultInputSource(); // 提供默认实现
          }

          core = new ThirdPersonControllerCore();
          core.SetInputSource(inputSource);
          // ... 设置其他依赖
      }
      catch (Exception e)
      {
          Debug.LogError($"[Player3C] Initialization failed: {e.Message}");
          enabled = false; // 禁用组件，避免后续错误
      }
  }
  ```

**常见问题**：
- ❌ **错误**：依赖为 null 时直接使用，导致 NullReferenceException
- ❌ **错误**：异常向上抛出，导致单帧错误影响整个系统
- ❌ **错误**：依赖缺失时系统完全停止，而不是降级处理

**实现建议**：
- 所有公共方法都应该进行空值检查
- Tick() 方法应该捕获异常并记录，但不中断游戏
- 依赖缺失时应该提供默认行为或降级处理
- 初始化失败时应该记录错误并禁用相关功能

### 13.12 资源管理和生命周期

**必须正确管理资源，避免内存泄漏和性能问题**：

- ✅ **事件订阅管理**：
  ```csharp
  // 使用 IDisposable 模式管理事件订阅
  public class RollMode : IPlayerMode, IDisposable
  {
      private IAnimationDriver animationDriver;
      private bool disposed = false;

      void OnEnter()
      {
          if (animationDriver != null)
          {
              animationDriver.OnOneShotFinished += OnRollFinished;
          }
      }

      void OnExit()
      {
          UnsubscribeEvents();
      }

      void Dispose()
      {
          if (!disposed)
          {
              UnsubscribeEvents();
              disposed = true;
          }
      }

      private void UnsubscribeEvents()
      {
          if (animationDriver != null)
          {
              animationDriver.OnOneShotFinished -= OnRollFinished;
          }
      }
  }

  // MonoBehaviour 组件在 OnDestroy 中释放
  void OnDestroy()
  {
      // 取消所有事件订阅
      if (core != null)
      {
          core.OnJumpConsumed -= HandleJump;
          core.OnInteractConsumed -= HandleInteract;
      }

      // 清理模式资源
      if (currentMode is IDisposable disposable)
      {
          disposable.Dispose();
      }
  }
  ```

- ✅ **对象池使用**：
  ```csharp
  // PlayerCommand、MotorState 等 struct 不需要池化（值类型）
  // 但临时对象（如事件参数）考虑使用对象池

  // 如果事件参数是 class，考虑使用对象池
  public class OneShotFinishedEventArgs
  {
      public string AnimationName { get; set; }

      // 使用对象池
      private static readonly ObjectPool<OneShotFinishedEventArgs> Pool =
          new ObjectPool<OneShotFinishedEventArgs>(() => new OneShotFinishedEventArgs());

      public static OneShotFinishedEventArgs Get(string animationName)
      {
          var args = Pool.Get();
          args.AnimationName = animationName;
          return args;
      }

      public void Return()
      {
          AnimationName = null;
          Pool.Return(this);
      }
  }
  ```

- ✅ **资源释放**：
  ```csharp
  // MonoBehaviour 组件实现资源释放
  public class ThirdPersonControllerMono : MonoBehaviour, IDisposable
  {
      private bool disposed = false;

      void OnDestroy()
      {
          Dispose();
      }

      public void Dispose()
      {
          if (!disposed)
          {
              // 释放核心系统
              if (core is IDisposable disposable)
              {
                  disposable.Dispose();
              }

              // 取消事件订阅
              UnsubscribeAllEvents();

              // 清理引用
              core = null;
              inputSource = null;
              motor = null;
              cameraRig = null;
              animationDriver = null;

              disposed = true;
          }
      }
  }

  // 纯 C# 类实现 IDisposable
  public class ThirdPersonControllerCore : IPlayerController, IDisposable
  {
      private bool disposed = false;

      public void Dispose()
      {
          if (!disposed)
          {
              // 清理事件订阅
              OnJumpConsumed = null;
              OnInteractConsumed = null;

              // 清理模式资源
              if (currentMode is IDisposable disposable)
              {
                  disposable.Dispose();
              }

              // 清理引用
              inputSource = null;
              motor = null;
              cameraRig = null;
              animationDriver = null;

              disposed = true;
          }
      }
  }
  ```

- ✅ **内存优化**：
  ```csharp
  // 避免每帧分配临时对象
  void Tick(float deltaTime)
  {
      // ❌ 错误：每帧分配新的 PlayerCommand（虽然 struct 在栈上，但频繁复制也有开销）
      // var cmd = new PlayerCommand { ... };

      // ✅ 正确：复用命令对象（如果使用 class）或使用 struct（当前设计）
      var cmd = inputSource.ReadCommand(); // struct 复制，开销小

      // ❌ 错误：每帧分配字符串
      // Debug.Log($"Mode: {CurrentMode.ToString()}");

      // ✅ 正确：使用缓存或条件编译
      #if UNITY_EDITOR && DEBUG
      if (Time.frameCount % 60 == 0) // 每 60 帧记录一次
      {
          Debug.Log($"Mode: {CurrentMode}");
      }
      #endif
  }
  ```

**常见问题**：
- ❌ **错误**：事件订阅后未取消，导致内存泄漏（Unity 项目高频事故）
- ❌ **错误**：每帧分配临时对象，导致 GC 压力
- ❌ **错误**：MonoBehaviour 销毁时未清理资源

**实现建议**：
- 所有事件订阅必须在 OnExit() 或 OnDestroy() 中取消
- 实现 IDisposable 模式管理资源
- 避免在 Tick() 中频繁分配对象
- 使用对象池管理临时对象（如果需要）

### 13.13 配置管理

**配置数据应该可配置、可验证**：

- ✅ **配置加载**：
  ```csharp
  // 使用 ScriptableObject 存储配置
  [CreateAssetMenu(fileName = "Player3CConfig", menuName = "Player3C/Config")]
  public class Player3CConfig : ScriptableObject
  {
      [Header("Locomotion")]
      public float defaultMaxSpeed = 5f;
      public float defaultTurnSpeed = 360f;

      [Header("Camera")]
      public CameraParams defaultCameraParams;

      [Header("Modes")]
      public LocomotionModeConfig locomotionConfig;
      public AimModeConfig aimConfig;
      // ...
  }

  // 模式配置
  [Serializable]
  public class LocomotionModeConfig
  {
      public float maxSpeed = 5f;
      public float turnSpeed = 360f;
      public CameraParams cameraParams;
  }

  // Controller 加载配置
  public class ThirdPersonControllerCore : IPlayerController
  {
      private Player3CConfig config;

      public void LoadConfig(Player3CConfig config)
      {
          if (config == null)
          {
              Debug.LogWarning("[Player3C] Config is null, using defaults");
              config = ScriptableObject.CreateInstance<Player3CConfig>();
          }

          this.config = config;
          ValidateConfig();
      }

      private void ValidateConfig()
      {
          if (config.defaultMaxSpeed < 0)
          {
              Debug.LogError("[Player3C] Invalid maxSpeed, using default");
              config.defaultMaxSpeed = 5f;
          }
          // ... 其他验证
      }
  }
  ```

- ✅ **运行时配置修改**：
  ```csharp
  // 支持运行时修改配置（用于调试和平衡调整）
  public class ThirdPersonControllerCore : IPlayerController
  {
      public void SetMaxSpeed(float speed)
      {
          if (speed < 0)
          {
              Debug.LogWarning($"[Player3C] Invalid speed: {speed}, clamping to 0");
              speed = 0;
          }

          if (currentMode != null)
          {
              // 更新当前模式的配置
              // 注意：这可能需要重新初始化模式
          }
      }
  }
  ```

- ✅ **配置验证**：
  ```csharp
  // 配置加载时验证参数范围
  public static class ConfigValidator
  {
      public static bool Validate(Player3CConfig config, out List<string> errors)
      {
          errors = new List<string>();

          if (config.defaultMaxSpeed < 0)
              errors.Add("defaultMaxSpeed must be >= 0");

          if (config.defaultTurnSpeed < 0)
              errors.Add("defaultTurnSpeed must be >= 0");

          if (config.defaultCameraParams.FOV <= 0 || config.defaultCameraParams.FOV > 180)
              errors.Add("FOV must be between 0 and 180");

          return errors.Count == 0;
      }
  }
  ```

**实现建议**：
- 使用 ScriptableObject 存储配置，便于在编辑器中调整
- 配置加载时进行验证，确保参数在合理范围内
- 提供默认配置，避免配置缺失导致错误
- 支持运行时修改配置（用于调试和平衡调整）

### 13.14 状态一致性

**必须保证状态一致性，支持回滚和恢复**：

- ✅ **模式切换失败处理**：
  ```csharp
  public void SetMode(PlayerMode newMode)
  {
      if (newMode == CurrentMode) return;

      var oldMode = currentMode;

      try
      {
          // 退出当前模式
          if (oldMode != null)
          {
              oldMode.OnExit();
          }

          // 获取新模式
          var modeInstance = GetModeInstance(newMode);
          if (modeInstance == null)
          {
              throw new ArgumentException($"Mode {newMode} not found");
          }

          // 进入新模式
          modeInstance.OnEnter();

          // 更新状态
          currentMode = modeInstance;
          currentModeType = newMode;

          // 更新相机参数
          var cameraParams = modeInstance.GetCameraParams();
          if (cameraRig != null)
          {
              cameraRig.SetCameraParams(cameraParams);
          }
      }
      catch (Exception e)
      {
          Debug.LogError($"[Player3C] Mode switch failed: {e.Message}");

          // 回滚到原模式
          if (oldMode != null)
          {
              try
              {
                  oldMode.OnEnter(); // 重新进入原模式
                  currentMode = oldMode;
                  currentModeType = oldMode.ModeType;
              }
              catch (Exception rollbackEx)
              {
                  Debug.LogError($"[Player3C] Rollback failed: {rollbackEx.Message}");
                  // 如果回滚也失败，切换到默认模式
                  SetMode(PlayerMode.Locomotion);
              }
          }
          else
          {
              // 如果没有原模式，切换到默认模式
              SetMode(PlayerMode.Locomotion);
          }
      }
  }
  ```

- ✅ **状态验证**：
  ```csharp
  // 定期验证系统状态的一致性
  public class ThirdPersonControllerCore : IPlayerController
  {
      private int validationFrameCount = 0;

      void Tick(float deltaTime)
      {
          // 每 60 帧验证一次状态
          if (++validationFrameCount >= 60)
          {
              validationFrameCount = 0;
              ValidateState();
          }

          // ... 正常更新逻辑
      }

      private void ValidateState()
      {
          // 验证模式一致性
          if (currentMode != null && currentMode.ModeType != currentModeType)
          {
              Debug.LogWarning($"[Player3C] Mode inconsistency detected: {currentMode.ModeType} != {currentModeType}");
              // 尝试修复
              currentModeType = currentMode.ModeType;
          }

          // 验证依赖有效性
          if (motor != null && !motor.IsEnabled)
          {
              Debug.LogWarning("[Player3C] Motor is disabled");
          }

          // 验证状态合理性
          var state = GetState();
          if (state.MaxSpeed < 0)
          {
              Debug.LogError("[Player3C] Invalid MaxSpeed, resetting to default");
              // 重置为默认值
          }
      }
  }
  ```

- ✅ **状态快照和恢复**：
  ```csharp
  // 状态快照（用于调试和回滚）
  public struct ControllerSnapshot
  {
      public PlayerMode Mode;
      public MotionPolicy MotionPolicy;
      public Vector3 Position;
      public Vector3 Velocity;
      public bool IsGrounded;
  }

  public class ThirdPersonControllerCore : IPlayerController
  {
      private ControllerSnapshot? lastSnapshot;

      public ControllerSnapshot CreateSnapshot()
      {
          var motorState = motor?.GetState() ?? default;
          return new ControllerSnapshot
          {
              Mode = CurrentMode,
              MotionPolicy = CurrentMotionPolicy,
              Position = motorState.Position,
              Velocity = motorState.Velocity,
              IsGrounded = motorState.IsGrounded
          };
      }

      public void RestoreSnapshot(ControllerSnapshot snapshot)
      {
          try
          {
              SetMode(snapshot.Mode);
              // ... 恢复其他状态
          }
          catch (Exception e)
          {
              Debug.LogError($"[Player3C] Restore snapshot failed: {e.Message}");
          }
      }
  }
  ```

**常见问题**：
- ❌ **错误**：模式切换失败时系统处于不一致状态
- ❌ **错误**：状态不一致时没有检测和修复机制
- ❌ **错误**：没有状态回滚能力，调试困难

**实现建议**：
- 模式切换失败时应该回滚到原模式或默认模式
- 定期验证系统状态的一致性
- 提供状态快照功能，便于调试和回滚
- 发现不一致时记录警告并尝试修复

### 13.15 线程安全

**Unity 主线程约束**：

- ⚠️ **所有 Tick() 方法必须在 Unity 主线程调用**
  - `Controller.Tick()`、`Motor.Tick()`、`Camera.Tick()`、`AnimationDriver.Tick()` 都必须在主线程
  - Unity API（如 `Transform`、`Rigidbody`、`Animator`）只能在主线程访问

- ⚠️ **所有 Unity API 调用必须在主线程**
  - 直接操作 `Transform.position`、`Rigidbody.velocity` 等必须在主线程
  - 读取 Unity 组件状态（如 `Animator.GetCurrentAnimatorStateInfo()`）必须在主线程

- ✅ **异步操作**：如果使用 async/await，确保回调回到主线程
  ```csharp
  // 异步操作示例
  async UniTask LoadConfigAsync()
  {
      var config = await LoadFromFileAsync();

      // 确保回到主线程再设置
      await UniTask.SwitchToMainThread();
      core.LoadConfig(config);
  }
  ```

- ✅ **多线程访问**：如果需要在其他线程访问系统状态，应使用线程安全的数据结构或消息队列
  ```csharp
  // 线程安全的状态查询（如果需要）
  public class ThreadSafeControllerState
  {
      private readonly object lockObject = new object();
      private ControllerState state;

      public ControllerState GetState()
      {
          lock (lockObject)
          {
              return state;
          }
      }

      public void UpdateState(ControllerState newState)
      {
          lock (lockObject)
          {
              state = newState;
          }
      }
  }
  ```

**常见问题**：
- ❌ **错误**：在后台线程调用 `Motor.Tick()` → 导致 Unity API 调用异常
- ❌ **错误**：在异步操作中直接访问 `Transform.position` → 导致线程安全异常
- ❌ **错误**：多线程同时修改共享状态 → 导致数据竞争

**实现建议**：
- 所有 Tick() 方法都应在 Unity 主线程调用（通过 MonoBehaviour 的 Update/LateUpdate）
- 如果需要在后台线程处理数据，应使用消息队列或线程安全的数据结构
- 异步操作完成后，确保回到主线程再访问 Unity API

---

## 附录：类图概览

```
IInputSource
    ↑
    ├─ NewInputSystemSource
    ├─ TouchInputSource
    └─ CompositeInputSource

IPlayerController
    ↑
    └─ ThirdPersonControllerCore

ThirdPersonControllerMono (MonoBehaviour)
    └─ 持有 ThirdPersonControllerCore

ICharacterMotor
    ↑
    ├─ CharacterControllerMotor
    └─ RigidbodyMotor

ICameraRig
    ↑
    ├─ CustomCameraRig
    └─ CinemachineCameraRig

IAnimationDriver
    ↑
    ├─ AnimatorDriver
    └─ PlayablesDriver

IPlayerMode
    ↑
    ├─ LocomotionMode
    ├─ AimMode
    ├─ LockOnMode
    └─ ...
```

---

**文档版本**: v1.0
**最后更新**: 2025
**维护者**: Miles

