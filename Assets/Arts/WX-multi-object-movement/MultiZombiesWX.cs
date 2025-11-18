using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class MultiZombiesWX : MonoBehaviour
{
    [Header("空间设置")]
    [SerializeField] float fieldSize = 200f;    // 空间尺寸为 fieldSize x fieldSize
    [SerializeField] int agentCount = 100;  // 空间中用于计算的的分布点数量，即“僵尸”数量的代理

    [Header("运动参数")]
    [Range(0.0f, 10f)][SerializeField] float maxSpeed = 5f;   // 移动的最大速度
    [Range(0.1f, 10f)][SerializeField] float gap = 1.0f;  // 分布点之间的最小间距，同时影响了网格单元的大小
    [SerializeField] float separationForce = 8f;    // 让分布点远离彼此的力的强度
    [Range(0.01f, 1.0f)][SerializeField] float stopSpeedThreshold = 0.05f;  // 当速度低于该值时，认为分布点已经停止
    [SerializeField] float stuckTimeToStop = 1.0f;  // 当速度低于阈值且持续时间超过该值时，认为分布点已经停止

    [Header("可视化")]
    [SerializeField] GameObject unitPrefab;  // 用于可视化的预制体


    // SoA for performance，注意，数组的索引对应同一个分布点
    float2[] positions;    // 所有分布点的位置
    float2[] velocities;   // 所有分布点的速度
    float2[] targets;      // 所有分布点的目标位置
    bool[] hasTarget;      // 所有分布点是否有目标位置
    float[] stuckTimers;   // 所有分布点的被卡住计时器
    Transform[] visuals;   // 所有分布点的可视化对象，用于驱动可视化对象的位置更新

    // cell grid for spatial partitioning
    float cellSize; // 网格单元的大小，约等于或大于 gap
    int gridDim;   // 网格的密度，即每行/列的单元格数量

    /// <summary>
    /// 声明一个二维数组，每个元素是一个整数列表 list，存储了位于该单元格内的分布点索引
    /// 例如，grid[2,3] 存储了所有位于 (2,3) 单元格内的分布点索引
    /// </summary>
    List<int>[,] grid;

    float halfField; // 应为 fieldSize 的一半，用于位置初始化和边界处理

    Unity.Mathematics.Random randomState;    // 随机数生成器

    void Start()
    {
        randomState = new Unity.Mathematics.Random((uint)(Time.time + 1));
        halfField = fieldSize * 0.5f;   // 计算 fieldSize 的一半
        InitSOA();
        InitCells();
        InitUnits();
    }

    // 初始化网格，注意 cellSize 不能为 0，且每个的 cell 最大不能超过 gap 的一半
    void InitCells()
    {
        cellSize = Mathf.Max(0.5f * gap, 0.1f);
        gridDim = Mathf.CeilToInt(fieldSize / cellSize);    // 计算网格密度，向上取整，如果 fieldSize=200，cellSize=1，则 gridDim=200
        grid = new List<int>[gridDim, gridDim];
        for (int x = 0; x < gridDim; x++)
        {
            for (int y = 0; y < gridDim; y++)
            {
                grid[x, y] = new List<int>(8); // 因为是 2D 邻寻，每个位置预留 8 个容量给相邻
            }
        }
    }

    // 初始化 SoA 数组，分配内存，cache 起来更快
    void InitSOA()
    {
        positions = new float2[agentCount];
        velocities = new float2[agentCount];
        targets = new float2[agentCount];
        hasTarget = new bool[agentCount];
        stuckTimers = new float[agentCount];
        visuals = new Transform[agentCount];
    }

    // 初始化单位，位置与目标随机分布
    void InitUnits()
    {
        for (int i = 0; i < agentCount; i++)
        {
            positions[i] = randomState.NextFloat2(-halfField, halfField);    // 注意这是一个 [) 区间，理论上应该完整包含 +-halfField 的
            velocities[i] = float2.zero;
            AssignRandomTargets(i);

            // 实例化可视化对象
            if (unitPrefab != null)
            {
                var zombie = Instantiate(unitPrefab, (Vector2)positions[i], Quaternion.identity, transform);
                zombie.name = $"Agent_{i}";
                visuals[i] = zombie.transform;
            }
        }
    }

    void AssignRandomTargets(int index)
    {
        targets[index] = randomState.NextFloat2(-halfField, halfField);
        hasTarget[index] = true;
        stuckTimers[index] = 0f;
    }

    void StepSimulation(float dt)
    {
        RebuildGrid();

        float gap2 = gap * gap;

        // 更新每个分布点的位置和速度
        for (int i = 0; i < agentCount; i++)
        {
            float2 position = positions[i];
            float2 velocity = velocities[i];


            if (!hasTarget[i])
            {
                velocities[i] = float2.zero;
                continue;
            }

            float2 target = targets[i];
            float2 direction = target - position;
            float distance2 = math.lengthsq(direction);    // 平方距离，以避免开方运算
            float2 vDesired = float2.zero;

            // 到达目标时，停止或者重新分配目标
            if (distance2 < gap2 * 0.25f)   // distance2 要与 (gap * 0.5) 的平方比较
            {
                hasTarget[i] = false;
                velocities[i] = float2.zero;
                continue;
            }
            else
            {
                vDesired = math.normalize(direction) * maxSpeed;
            }

            // 计算避让速度
            float2 vAvoid = float2.zero;

            GetCell(position, out int cellX, out int cellY);

            // 计算每个格子和其周围的 8 个格子
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                int neighborX = cellX + offsetX;
                if (neighborX < 0 || neighborX >= gridDim) continue;

                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    int neighborY = cellY + offsetY;
                    if (neighborY < 0 || neighborY >= gridDim) continue;

                    // 获取该单元格内的所有分布点索引
                    List<int> cellList = grid[neighborX, neighborY];
                    for (int idx = 0; idx < cellList.Count; idx++)
                    {
                        int j = cellList[idx];
                        if (j == i) continue; // 跳过自己

                        float2 neighborPosition = positions[j];
                        float2 toNeighborDir = neighborPosition - position;
                        float distToNeighbor2 = math.lengthsq(toNeighborDir);   // 平方距离，以避免开方运算
                        if (distToNeighbor2 >= gap2 || distToNeighbor2 < 1e-6f) // 避免除以零
                        {
                            continue;
                        }
                        float d = math.sqrt(distToNeighbor2);   // 实际距离
                        float pushForce = (gap - d) / gap;  // 归一化推力，靠的越近推力越大
                        float2 pushDir = -toNeighborDir / d; // 归一化方向，指向自己
                        vAvoid += pushDir * pushForce * separationForce;
                    }
                }
            }

            float2 vNew = vDesired + vAvoid;

            // 限制最大速度
            float speed2 = math.lengthsq(vNew);  // 平方速度，以避免开方运算
            if (speed2 > maxSpeed * maxSpeed)
            {
                float speed = math.sqrt(speed2);
                vNew *= (maxSpeed / speed);
                speed2 = maxSpeed * maxSpeed;
            }

            if (speed2 < stopSpeedThreshold * stopSpeedThreshold && distance2 > gap2)
            {
                stuckTimers[i] += dt;
                if (stuckTimers[i] >= stuckTimeToStop)
                {
                    hasTarget[i] = false;
                    vNew = float2.zero;
                }
            }
            else
            {
                stuckTimers[i] = 0f;
            }

            position += vNew * dt;

            position.x = math.clamp(position.x, -halfField, halfField);
            position.y = math.clamp(position.y, -halfField, halfField);

            positions[i] = position;
            velocities[i] = vNew;

        }
    }

    void RebuildGrid()
    {
        // 清空网格，穷举所有单元格
        for (int x = 0; x < gridDim; x++)
        {
            for (int y = 0; y < gridDim; y++)
            {
                grid[x, y].Clear();
            }
        }

        // 将所有分布点重新分配到网格单元中，此处暂不考虑所有分布点都位于同一单元格的极端情况，后续处理
        for (int i = 0; i < agentCount; i++)
        {
            GetCell(positions[i], out int cellX, out int cellY);
            grid[cellX, cellY].Add(i);
        }
    }

    void GetCell(float2 position, out int cellX, out int cellY)
    {
        float x01 = (position.x + halfField) / fieldSize;       // 将 position.x 归一化到 0-1 范围
        float y01 = (position.y + halfField) / fieldSize;       // 将 position.y 归一化到 0-1 范围

        cellX = Mathf.Clamp(Mathf.FloorToInt(x01 * gridDim), 0, gridDim - 1);   // 求得 x 方向上的网格索引
        cellY = Mathf.Clamp(Mathf.FloorToInt(y01 * gridDim), 0, gridDim - 1);   // 求得 y 方向上的网格索引
    }

}
