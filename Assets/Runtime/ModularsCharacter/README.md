# 模块化角色系统

本系统展示了**数据与逻辑分离**的设计模式，采用依赖注入（Dependency Injection）和接口抽象，实现高内聚、低耦合的模块化角色换装系统。

## 文件说明

### 核心接口与数据类

#### 1. `IModularChar.cs` - 数据接口
- **职责**：定义模块化角色数据的访问接口
- **设计要点**：
  - 提供受控的公共访问方法（`TryGetBone`, `HasPart` 等）
  - 使用 `internal` 关键字暴露内部字典，仅供 System 类使用
  - 外部代码无法直接修改数据，必须通过 System 类
  - 支持查询骨骼和部位信息

#### 2. `ModularCharMonoRef.cs` - 数据组件
- **职责**：纯数据类，实现 `IModularChar` 接口
- **设计要点**：
  - 所有字段都是私有的
  - 通过接口方法提供受控访问
  - 不包含任何业务逻辑
  - 继承自 `MonoBehaviour`，可挂载到 GameObject 上

#### 3. `ModularPartType.cs` - 部位类型枚举
- **职责**：定义所有可更换的部位类型
- **包含类型**：Rigid, Hair, Head, Outfit, UpperBody, LowerBody, Shoes, Gloves, Backpack, Accessory

### 系统类（业务逻辑）

#### 4. `ModularBoneSystem.cs` - 骨骼管理系统
- **职责**：管理骨骼映射、验证、重置、重绑定
- **主要方法**：
  - `VerifyBoneMap(IModularChar)` - 验证并初始化骨骼映射，返回是否成功
  - `ResetBoneMap(IModularChar)` - 重置骨骼映射，移除额外骨骼
  - `RebindBones(SkinnedMeshRenderer, IModularChar)` - 重新绑定 SkinnedMeshRenderer 的骨骼，支持额外骨骼（如尾巴）
  - `RemoveOldBones(Transform, IModularChar)` - 移除旧骨骼

#### 5. `ModularEquipmentSystem.cs` - 装备管理系统
- **职责**：管理装备的更换、移除
- **依赖**：需要 `ModularBoneSystem` 实例（通过构造函数注入）
- **主要方法**：
  - `ChangeRigidPart()` - 更换刚体部位（如武器、配饰）
  - `ChangeSkinnedPart()` - 更换蒙皮部位（如服装、头发）
  - `RemovePart()` - 移除指定部位
  - `RemoveAllParts()` - 移除所有部位
  - `ChangePartsBatch()` - 批量更换部位（用于套装）

## 使用示例

### 基础使用

```csharp
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    [SerializeField] private ModularCharMonoRef charData;
    [SerializeField] private GameObject upperBodyPrefab;
    [SerializeField] private GameObject accessoryPrefab;

    private ModularBoneSystem boneSystem;
    private ModularEquipmentSystem equipSystem;

    void Start()
    {
        // 1. 获取或添加数据组件
        charData = GetComponent<ModularCharMonoRef>();
        if (charData == null)
        {
            charData = gameObject.AddComponent<ModularCharMonoRef>();
        }

        // 2. 初始化系统（使用依赖注入）
        boneSystem = new ModularBoneSystem();
        equipSystem = new ModularEquipmentSystem(boneSystem); // 注入依赖

        // 3. 验证骨骼映射（必须先执行）
        if (!boneSystem.VerifyBoneMap(charData))
        {
            Debug.LogError("骨骼映射验证失败！");
            return;
        }

        // 4. 更换装备
        equipSystem.ChangeSkinnedPart(charData, ModularPartType.UpperBody, upperBodyPrefab);
        equipSystem.ChangeRigidPart(charData, ModularPartType.Accessory, accessoryPrefab);

        // 5. 查询数据（通过接口）
        if (charData.TryGetPart(ModularPartType.UpperBody, out GameObject part))
        {
            Debug.Log($"已装备: {part.name}");
        }
    }
}
```

### 批量更换（套装）

```csharp
// 批量更换多个部位（如套装）
var parts = new Dictionary<ModularPartType, GameObject>
{
    { ModularPartType.UpperBody, upperBodyPrefab },
    { ModularPartType.LowerBody, lowerBodyPrefab },
    { ModularPartType.Shoes, shoesPrefab }
};

int successCount = equipSystem.ChangePartsBatch(charData, parts, isSkinned: true);
Debug.Log($"成功更换 {successCount} 个部位");
```

### 移除装备

```csharp
// 移除单个部位
equipSystem.RemovePart(charData, ModularPartType.UpperBody);

// 移除所有部位
equipSystem.RemoveAllParts(charData);
```

## 设计优势

1. **职责分离**：数据与逻辑完全分离，易于维护和测试
2. **依赖注入**：通过构造函数注入依赖，符合 SOLID 原则，提高可测试性
3. **封装性**：外部代码无法直接修改内部数据，必须通过 System 类
4. **可扩展性**：可以轻松添加新的 System 类（如 `ModularAnimationSystem`、`ModularColorSystem`）
5. **可测试性**：System 类可以独立测试，可以注入 mock 对象
6. **类型安全**：接口提供了类型安全的访问方式
7. **单一职责**：每个 System 类只负责一个特定领域的功能

## 架构设计

### 设计模式

1. **数据与逻辑分离（Data-Logic Separation）**
   - 数据类（`ModularCharMonoRef`）只存储数据，不包含业务逻辑
   - 系统类（`ModularBoneSystem`、`ModularEquipmentSystem`）只包含业务逻辑，不存储数据

2. **依赖注入（Dependency Injection）**
   - `ModularEquipmentSystem` 通过构造函数接收 `ModularBoneSystem` 依赖
   - 提高可测试性和灵活性

3. **接口抽象（Interface Abstraction）**
   - 通过 `IModularChar` 接口定义数据访问契约
   - 使用 `internal` 关键字保护内部实现细节

### 与旧设计的对比

#### 旧设计的问题
- `ModularCharMono` 直接暴露 `Dictionary`，外部可以随意修改
- `ModularDisguiseSystem` 只有一个方法，职责不清晰
- 业务逻辑仍在 `ModularChar` 中，未完全分离
- 系统类内部创建依赖，难以测试

#### 新设计的改进
- ✅ 接口提供受控访问，保护内部数据
- ✅ 系统类职责单一，易于理解和维护
- ✅ 完全分离数据和逻辑，符合单一职责原则
- ✅ 使用依赖注入，提高可测试性和灵活性
- ✅ 所有方法都有返回值和完善的错误处理

## 注意事项

### 1. internal 关键字
- System 类需要与数据类在同一个程序集中（Assembly-CSharp）
- 如果需要在不同程序集中使用，可以考虑：
  - 使用两个接口（公共接口 + 内部接口）
  - 使用 `InternalsVisibleTo` 属性
  - 让 System 类直接访问实现类的内部成员

### 2. 初始化顺序
```csharp
// 正确的初始化顺序：
// 1. 创建数据组件
// 2. 创建系统实例（依赖注入）
// 3. 验证骨骼映射（必须先执行）
// 4. 更换装备
```

### 3. 性能考虑
- 骨骼映射验证只需执行一次（在首次换装前）
- 批量更换时，`resetBoneMap` 参数设为 `false`，最后统一重置
- 如果频繁访问，可以考虑添加缓存机制

### 4. 错误处理
- 所有方法都包含错误检查和日志输出
- 方法返回 `bool` 值表示操作是否成功
- 构造函数会检查依赖是否为 null，抛出 `ArgumentNullException`

### 5. 线程安全
- 当前实现不是线程安全的
- 如需多线程访问，需要添加同步机制

## 扩展建议

### 可以添加的系统类
- `ModularAnimationSystem` - 管理动画相关逻辑
- `ModularColorSystem` - 管理颜色/材质更换
- `ModularPhysicsSystem` - 管理物理相关逻辑
- `ModularAudioSystem` - 管理音效相关逻辑

### 可以添加的功能
- 装备预设（Outfit Preset）- 保存和加载整套装备配置
- 装备验证 - 检查装备是否兼容
- 装备事件 - 装备更换时触发事件通知
- 装备持久化 - 保存装备配置到文件或数据库

