# 角色 3C 系统设计文档

## 一、总体设计目标

### 1.1 核心原则

1. **输入无关性**
   - 键盘 / 手柄 / 触控 → 统一抽象为玩家意图
   - 任何新设备都不应影响运动学与相机逻辑

2. **运动权威清晰**
   - 角色位移与旋转只能通过 Motor 这一条通道发生
   - 动画、输入、相机均不得直接改 Transform 位移

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

---

## 三、核心接口设计

### 3.1 输入层接口

#### `IInputSource`
输入源抽象接口，负责将设备输入归一化为统一格式。

```csharp
/// <summary>
/// 输入源接口 - 负责将设备输入转换为 PlayerCommand
/// </summary>
public interface IInputSource
{
    /// <summary>当前控制方案（键盘鼠标/手柄/触控）</summary>
    ControlScheme CurrentScheme { get; }
    
    /// <summary>读取当前帧的玩家命令</summary>
    PlayerCommand ReadCommand();
    
    /// <summary>是否启用</summary>
    bool IsEnabled { get; set; }
}
```

#### `PlayerCommand`
玩家意图的统一数据结构。

```csharp
/// <summary>
/// 玩家命令 - 统一的意图协议
/// </summary>
public struct PlayerCommand
{
    /// <summary>移动输入（归一化的 Vector2，范围 [-1, 1]）</summary>
    public Vector2 Move;
    
    /// <summary>视角输入（归一化的 Vector2，范围 [-1, 1]）</summary>
    public Vector2 Look;
    
    /// <summary>跳跃按钮</summary>
    public bool Jump;
    
    /// <summary>冲刺按钮</summary>
    public bool Sprint;
    
    /// <summary>瞄准按钮</summary>
    public bool Aim;
    
    /// <summary>交互按钮</summary>
    public bool Interact;
    
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
/// </summary>
public interface IPlayerController
{
    /// <summary>当前玩家模式</summary>
    PlayerMode CurrentMode { get; }
    
    /// <summary>当前运动策略</summary>
    MotionPolicy CurrentMotionPolicy { get; }
    
    /// <summary>更新控制器（每帧调用）</summary>
    void Tick(float deltaTime);
    
    /// <summary>设置输入源</summary>
    void SetInputSource(IInputSource inputSource);
    
    /// <summary>切换玩家模式</summary>
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

```csharp
/// <summary>
/// 角色运动接口 - 位移和旋转的唯一权威
/// </summary>
public interface ICharacterMotor
{
    /// <summary>当前速度</summary>
    Vector3 Velocity { get; }
    
    /// <summary>是否在地面</summary>
    bool IsGrounded { get; }
    
    /// <summary>是否在移动</summary>
    bool IsMoving { get; }
    
    /// <summary>当前运动策略</summary>
    MotionPolicy CurrentPolicy { get; }
    
    /// <summary>更新运动（每帧调用）</summary>
    void Tick(float deltaTime);
    
    /// <summary>设置移动意图（来自 Controller）</summary>
    void SetMoveIntent(Vector3 moveDirection, float speed);
    
    /// <summary>设置旋转意图（来自 Controller）</summary>
    void SetRotationIntent(Quaternion rotation);
    
    /// <summary>执行跳跃</summary>
    void Jump(float jumpForce);
    
    /// <summary>设置运动策略</summary>
    void SetMotionPolicy(MotionPolicy policy);
    
    /// <summary>应用 RootMotion 位移（当策略为 RootMotionAuthority 时）</summary>
    void ApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation);
    
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

```csharp
/// <summary>
/// 相机接口 - 视角响应器
/// </summary>
public interface ICameraRig
{
    /// <summary>相机 Transform</summary>
    Transform CameraTransform { get; }
    
    /// <summary>Unity Camera 组件</summary>
    Camera Camera { get; }
    
    /// <summary>更新相机（每帧调用）</summary>
    void Tick(float deltaTime);
    
    /// <summary>设置视角输入（来自 Controller）</summary>
    void SetLookInput(Vector2 lookInput);
    
    /// <summary>设置跟随目标</summary>
    void SetTarget(Transform target);
    
    /// <summary>设置相机参数（根据模式调整）</summary>
    void SetCameraParams(CameraParams parameters);
    
    /// <summary>重置相机到默认位置</summary>
    void ResetCamera();
}
```

#### `CameraParams`
相机参数配置。

```csharp
/// <summary>
/// 相机参数配置
/// </summary>
public struct CameraParams
{
    public float FOV;
    public float Distance;
    public float Height;
    public Vector3 Offset;
    public float Sensitivity;
    public float PitchMin;
    public float PitchMax;
    public bool InvertY;
}
```

---

### 3.5 动画层接口

#### `IAnimationDriver`
动画驱动接口，负责动画表现。

```csharp
/// <summary>
/// 动画驱动接口 - 动画表现层
/// </summary>
public interface IAnimationDriver
{
    /// <summary>是否启用</summary>
    bool IsEnabled { get; set; }
    
    /// <summary>更新动画（每帧调用）</summary>
    void Tick(float deltaTime);
    
    /// <summary>设置运动状态（来自 Motor）</summary>
    void SetMotorState(MotorState state);
    
    /// <summary>设置控制器状态（来自 Controller）</summary>
    void SetControllerState(ControllerState state);
    
    /// <summary>播放一次性动画（如翻滚、攀爬）</summary>
    void PlayOneShot(string animationName, float transitionTime = 0.1f);
    
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
    
    /// <summary>获取相机参数</summary>
    CameraParams GetCameraParams();
    
    /// <summary>是否可以移动</summary>
    bool CanMove();
    
    /// <summary>是否可以旋转</summary>
    bool CanRotate();
    
    /// <summary>是否可以跳跃</summary>
    bool CanJump();
    
    /// <summary>进入模式时调用</summary>
    void OnEnter();
    
    /// <summary>退出模式时调用</summary>
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

```csharp
/// <summary>
/// New Input System 输入源实现
/// 支持键盘鼠标和手柄
/// </summary>
public class NewInputSystemSource : MonoBehaviour, IInputSource
{
    // 依赖：Unity Input System
    // 职责：将 InputAction 转换为 PlayerCommand
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

#### `ThirdPersonController`
第三人称控制器核心实现。

```csharp
/// <summary>
/// 第三人称控制器
/// 规则中枢，负责编排和决策
/// </summary>
public class ThirdPersonController : MonoBehaviour, IPlayerController
{
    // 依赖：IInputSource, ICharacterMotor, ICameraRig, IAnimationDriver
    // 职责：
    // 1. 读取 PlayerCommand
    // 2. 决定当前 PlayerMode
    // 3. 决定 MotionPolicy
    // 4. 分发指令给 Motor / Camera / AnimationDriver
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
/// </summary>
public class CustomCameraRig : MonoBehaviour, ICameraRig
{
    // 职责：实现第三人称相机跟随和视角控制
}
```

#### `CinemachineCameraRig`
Cinemachine 相机实现。

```csharp
/// <summary>
/// Cinemachine 相机实现
/// </summary>
public class CinemachineCameraRig : MonoBehaviour, ICameraRig
{
    // 依赖：Cinemachine
    // 职责：封装 Cinemachine 为 ICameraRig 接口
}
```

---

### 4.5 动画层实现类

#### `AnimatorDriver`
基于 Animator 的动画驱动实现。

```csharp
/// <summary>
/// Animator 动画驱动实现
/// </summary>
public class AnimatorDriver : MonoBehaviour, IAnimationDriver
{
    // 依赖：Animator
    // 职责：通过 Animator 控制动画播放
}
```

#### `PlayablesDriver`
基于 Playables 的动画驱动实现。

```csharp
/// <summary>
/// Playables 动画驱动实现
/// </summary>
public class PlayablesDriver : MonoBehaviour, IAnimationDriver
{
    // 依赖：PlayableGraph
    // 职责：通过 PlayableGraph 控制动画播放
}
```

---

## 五、数据流设计

### 5.1 正常移动流程

```
1. InputSource.ReadCommand()
   ↓
2. ThirdPersonController.Tick()
   ├─ 读取 PlayerCommand
   ├─ 根据模式决定规则
   ├─ 分发指令：
   │  ├─ Motor.SetMoveIntent()
   │  ├─ Motor.SetRotationIntent()
   │  ├─ Camera.SetLookInput()
   │  └─ AnimationDriver.SetMotorState()
   ↓
3. Motor.Tick()
   ├─ 执行位移计算
   ├─ 处理碰撞
   └─ 更新 MotorState
   ↓
4. Camera.Tick()
   ├─ 处理视角输入
   └─ 更新相机位置
   ↓
5. AnimationDriver.Tick()
   ├─ 读取 MotorState
   ├─ 更新动画参数
   └─ 播放动画
```

### 5.2 RootMotion 流程

```
1. AnimationDriver.GetRootMotionDelta()
   ↓
2. Motor.ApplyRootMotion()
   ├─ 接收 RootMotion 增量
   ├─ 应用碰撞约束
   └─ 更新位置
```

### 5.3 模式切换流程

```
1. Controller.SetMode(newMode)
   ↓
2. 当前模式 OnExit()
   ↓
3. 新模式 OnEnter()
   ↓
4. Controller 更新规则
   ├─ 更新 MotionPolicy
   ├─ 更新速度限制
   └─ 更新相机参数
```

---

## 六、扩展点设计

### 6.1 输入扩展

**添加新输入设备**：
1. 实现 `IInputSource` 接口
2. 在 `CompositeInputSource` 中注册
3. 无需修改其他系统

### 6.2 运动扩展

**切换运动实现**：
1. 实现 `ICharacterMotor` 接口
2. 在 Controller 中替换引用
3. 无需修改其他系统

### 6.3 相机扩展

**切换相机实现**：
1. 实现 `ICameraRig` 接口
2. 在 Controller 中替换引用
3. 无需修改其他系统

### 6.4 动画扩展

**切换动画实现**：
1. 实现 `IAnimationDriver` 接口
2. 在 Controller 中替换引用
3. 无需修改其他系统

### 6.5 模式扩展

**添加新模式**：
1. 实现 `IPlayerMode` 接口
2. 在 Controller 中注册
3. 通过 `SetMode()` 切换

---

## 七、生命周期管理

### 7.1 MonoBehaviour 职责

MonoBehaviour 只负责：
- 组件引用管理
- Awake/Start 装配
- Update 驱动 Tick

### 7.2 核心系统初始化

```csharp
public class ThirdPersonControllerMono : MonoBehaviour
{
    private ThirdPersonController controller;
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
        
        // 创建控制器（纯 C# 类）
        controller = new ThirdPersonController();
        controller.SetInputSource(inputSource);
        // ... 设置其他依赖
    }
    
    void Update()
    {
        controller.Tick(Time.deltaTime);
    }
}
```

---

## 八、测试策略

### 8.1 单元测试

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

### 8.2 集成测试

通过接口 Mock 测试系统间交互：

```csharp
[Test]
public void TestControllerIntegration()
{
    var mockInput = new Mock<IInputSource>();
    var mockMotor = new Mock<ICharacterMotor>();
    var controller = new ThirdPersonController();
    
    controller.SetInputSource(mockInput.Object);
    controller.Tick(0.016f);
    
    // 验证 mockMotor 被正确调用
}
```

---

## 九、性能考虑

### 9.1 对象分配

- `PlayerCommand` 使用 `struct` 避免堆分配
- `MotorState`、`ControllerState` 使用 `struct`
- 考虑对象池管理临时对象

### 9.2 更新频率

- Motor、Camera 每帧更新
- AnimationDriver 每帧更新
- Controller 每帧更新
- InputSource 每帧读取

---

## 十、调试支持

### 10.1 可视化调试

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

### 10.2 日志系统

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

## 十一、实现优先级

### 阶段一：核心框架（MVP）
1. ✅ `IInputSource` 接口 + `NewInputSystemSource` 实现
2. ✅ `PlayerCommand` 结构
3. ✅ `ICharacterMotor` 接口 + `CharacterControllerMotor` 实现
4. ✅ `ICameraRig` 接口 + `CustomCameraRig` 实现
5. ✅ `IAnimationDriver` 接口 + `AnimatorDriver` 实现
6. ✅ `ThirdPersonController` 基础实现
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

## 十二、注意事项

### 12.1 依赖注入

所有依赖通过构造函数或 Setter 注入，避免在类内部查找组件。

### 12.2 接口隔离

每个接口只暴露必要的方法，避免接口过于庞大。

### 12.3 单一职责

每个类只负责一件事，保持高内聚。

### 12.4 依赖方向

严格遵守单向依赖，禁止反向引用。

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
    └─ ThirdPersonController

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
**最后更新**: 2024  
**维护者**: 开发团队

