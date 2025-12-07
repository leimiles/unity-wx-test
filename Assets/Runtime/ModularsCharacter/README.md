# 模块化角色系统 - 参考实现

本目录包含模块化角色系统的参考实现，展示了**数据与逻辑分离**的设计模式。

## 文件说明

### 1. `IModularChar.cs` - 数据接口
- **职责**：定义模块化角色数据的访问接口
- **设计要点**：
  - 提供受控的公共访问方法（`TryGetBone`, `HasPart` 等）
  - 使用 `internal` 关键字暴露内部字典，仅供 System 类使用
  - 外部代码无法直接修改数据，必须通过 System 类

### 2. `ModularCharMonoRef.cs` - 数据组件
- **职责**：纯数据类，实现 `IModularChar` 接口
- **设计要点**：
  - 所有字段都是私有的
  - 通过接口方法提供受控访问
  - 不包含任何业务逻辑

### 3. `ModularBoneSystem.cs` - 骨骼管理系统
- **职责**：管理骨骼映射、验证、重置、重绑定
- **主要方法**：
  - `VerifyBoneMap()` - 验证并初始化骨骼映射
  - `ResetBoneMap()` - 重置骨骼映射，移除额外骨骼
  - `RebindBones()` - 重新绑定 SkinnedMeshRenderer 的骨骼
  - `RemoveOldBones()` - 移除旧骨骼

### 4. `ModularEquipmentSystem.cs` - 装备管理系统
- **职责**：管理装备的更换、移除
- **主要方法**：
  - `ChangeRigidPart()` - 更换刚体部位
  - `ChangeSkinnedPart()` - 更换蒙皮部位
  - `RemovePart()` - 移除部位
  - `ChangePartsBatch()` - 批量更换部位

## 使用示例

```csharp
// 1. 在 GameObject 上添加 ModularCharMonoRef 组件
ModularCharMonoRef charData = gameObject.AddComponent<ModularCharMonoRef>();

// 2. 初始化骨骼映射
ModularBoneSystem boneSystem = new ModularBoneSystem();
boneSystem.VerifyBoneMap(charData);

// 3. 更换装备
ModularEquipmentSystem equipSystem = new ModularEquipmentSystem();
equipSystem.ChangeSkinnedPart(charData, ModularPartType.UpperBody, upperBodyPrefab);
equipSystem.ChangeRigidPart(charData, ModularPartType.Accessory, accessoryPrefab, attachmentPoint);

// 4. 查询数据（通过接口）
if (charData.TryGetPart(ModularPartType.UpperBody, out GameObject part))
{
    Debug.Log($"已装备: {part.name}");
}
```

## 设计优势

1. **职责分离**：数据与逻辑完全分离，易于维护和测试
2. **封装性**：外部代码无法直接修改内部数据
3. **可扩展性**：可以轻松添加新的 System 类（如 `ModularAnimationSystem`）
4. **可测试性**：System 类可以独立测试，不依赖 Unity 环境
5. **类型安全**：接口提供了类型安全的访问方式

## 与现有代码的对比

### 现有设计的问题
- `ModularCharMono` 直接暴露 `Dictionary`，外部可以随意修改
- `ModularDisguiseSystem` 只有一个方法，职责不清晰
- 业务逻辑仍在 `ModularChar` 中，未完全分离

### 参考设计的改进
- 接口提供受控访问，保护内部数据
- 系统类职责单一，易于理解和维护
- 完全分离数据和逻辑，符合单一职责原则

## 注意事项

1. **internal 关键字**：需要将 System 类放在 `Assembly-CSharp` 或与数据类相同的程序集中
2. **性能考虑**：如果频繁访问，可以考虑添加缓存机制
3. **错误处理**：所有方法都包含错误检查和日志输出

