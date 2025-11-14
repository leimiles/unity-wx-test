using System.Collections.Generic;
using UnityEngine;

public class MultiZombiesMovement : MonoBehaviour
{
    [Header("场地设置")]
    public float fieldSize = 200f;
    public int agentCount = 100;

    [Header("运动参数")]
    public float maxSpeed = 5f;
    public float gap = 1.0f;
    public float separationForce = 8f;
    public float stopSpeedThreshold = 0.05f;
    public float stuckTimeToStop = 1.0f;

    [Header("可视化")]
    public GameObject unitPrefab;


    Vector2[] positions;
    Vector2[] velocities;
    Vector2[] targets;
    bool[] hasTarget;
    float[] stuckTimers;
    Transform[] visuals;


    float cellSize;
    int gridDim;
    List<int>[,] grid;

    float halfField;

    void Start()
    {
        Application.targetFrameRate = 60;

        halfField = fieldSize * 0.5f;

        positions = new Vector2[agentCount];
        velocities = new Vector2[agentCount];
        targets = new Vector2[agentCount];
        hasTarget = new bool[agentCount];
        stuckTimers = new float[agentCount];
        visuals = new Transform[agentCount];


        cellSize = Mathf.Max(0.5f * gap, 0.1f);          // 避免为 0
        gridDim = Mathf.CeilToInt(fieldSize / cellSize);
        grid = new List<int>[gridDim, gridDim];
        for (int x = 0; x < gridDim; x++)
            for (int y = 0; y < gridDim; y++)
                grid[x, y] = new List<int>(8); // 预留一点容量


        for (int i = 0; i < agentCount; i++)
        {
            var pos = new Vector2(
                Random.Range(-halfField, halfField),
                Random.Range(-halfField, halfField)
            );
            positions[i] = pos;
            velocities[i] = Vector2.zero;
            AssignRandomTarget(i);

            if (unitPrefab != null)
            {
                var go = Instantiate(unitPrefab, pos, Quaternion.identity, transform);
                go.name = $"Agent_{i}";
                visuals[i] = go.transform;
            }
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        StepSimulation(dt);
        SyncVisuals();
    }

    // 随机目标
    void AssignRandomTarget(int i)
    {
        targets[i] = new Vector2(
            Random.Range(-halfField, halfField),
            Random.Range(-halfField, halfField)
        );
        hasTarget[i] = true;
        stuckTimers[i] = 0f;
    }


    void StepSimulation(float dt)
    {

        RebuildGrid();

        float gapSq = gap * gap;


        for (int i = 0; i < agentCount; i++)
        {
            Vector2 pos = positions[i];
            Vector2 vel = velocities[i];


            if (!hasTarget[i])
            {
                velocities[i] = Vector2.zero;
                continue;
            }

            Vector2 target = targets[i];
            Vector2 toTarget = target - pos;
            float distToTarget = toTarget.magnitude;
            Vector2 vDesired = Vector2.zero;


            if (distToTarget < gap * 0.5f)
            {
                // hasTarget[i] = false;  // 暂时停止
                // velocities[i] = Vector2.zero;
                // continue;
                AssignRandomTarget(i);
            }
            else
            {

                vDesired = toTarget.normalized * maxSpeed;
            }


            Vector2 vAvoid = Vector2.zero;

            GetCell(pos, out int cx, out int cy);

            for (int ox = -1; ox <= 1; ox++)
            {
                int nx = cx + ox;
                if (nx < 0 || nx >= gridDim) continue;

                for (int oy = -1; oy <= 1; oy++)
                {
                    int ny = cy + oy;
                    if (ny < 0 || ny >= gridDim) continue;

                    List<int> cellList = grid[nx, ny];
                    for (int idx = 0; idx < cellList.Count; idx++)
                    {
                        int j = cellList[idx];
                        if (j == i) continue;

                        Vector2 otherPos = positions[j];
                        Vector2 diff = otherPos - pos;
                        float dSq = diff.sqrMagnitude;
                        if (dSq >= gapSq || dSq < 1e-6f) continue;

                        float d = Mathf.Sqrt(dSq);

                        float push = (gap - d) / gap; // 0~1
                        Vector2 dirAway = -(diff / d);
                        vAvoid += dirAway * push * separationForce;
                    }
                }
            }


            Vector2 vNew = vDesired + vAvoid;


            float speed = vNew.magnitude;
            if (speed > maxSpeed)
            {
                vNew = vNew * (maxSpeed / speed);
                speed = maxSpeed;
            }


            if (speed < stopSpeedThreshold && distToTarget > gap)
            {
                stuckTimers[i] += dt;
                if (stuckTimers[i] >= stuckTimeToStop)
                {

                    hasTarget[i] = false;
                    vNew = Vector2.zero;
                }
            }
            else
            {
                stuckTimers[i] = 0f;
            }


            pos += vNew * dt;


            pos.x = Mathf.Clamp(pos.x, -halfField, halfField);
            pos.y = Mathf.Clamp(pos.y, -halfField, halfField);


            positions[i] = pos;
            velocities[i] = vNew;
        }
    }


    void RebuildGrid()
    {

        for (int x = 0; x < gridDim; x++)
            for (int y = 0; y < gridDim; y++)
                grid[x, y].Clear();


        for (int i = 0; i < agentCount; i++)
        {
            GetCell(positions[i], out int cx, out int cy);
            grid[cx, cy].Add(i);
        }
    }


    void GetCell(Vector2 pos, out int cx, out int cy)
    {
        float x01 = (pos.x + halfField) / fieldSize; // [-half, half] -> [0,1]
        float y01 = (pos.y + halfField) / fieldSize;

        cx = Mathf.Clamp(Mathf.FloorToInt(x01 * gridDim), 0, gridDim - 1);
        cy = Mathf.Clamp(Mathf.FloorToInt(y01 * gridDim), 0, gridDim - 1);
    }


    void SyncVisuals()
    {
        for (int i = 0; i < agentCount; i++)
        {
            if (visuals[i] == null) continue;

            visuals[i].position = new Vector3(positions[i].x, positions[i].y, 0f);

            Vector2 v = velocities[i];
            if (v.sqrMagnitude > 1e-4f)
            {
                float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                visuals[i].rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(fieldSize, fieldSize, 0));
    }
}
