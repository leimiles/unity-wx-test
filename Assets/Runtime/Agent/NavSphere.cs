using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class NavSphere : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float minWaitTime = 1f;      // 到达目标后最小等待时间
    [SerializeField] private float maxWaitTime = 3f;      // 到达目标后最大等待时间
    [SerializeField] private float searchRadius = 10f;    // 搜索随机点的半径

    [Header("旋转设置")]
    [SerializeField] private bool enableRollingVisual = true;  // 是否启用视觉滚动效果
    [SerializeField] private float rollingSpeedMultiplier = 1f; // 滚动速度倍数

    private NavMeshAgent agent;
    private Vector3 targetPosition;
    private bool hasTarget = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // 设置 NavMeshAgent 属性，适合球的移动
        agent.acceleration = 8f;
        agent.angularSpeed = 180f;

        // 寻找第一个随机目标
        FindRandomDestination();
    }

    void Update()
    {
        // 检查是否到达目标点
        if (hasTarget && !agent.pathPending)
        {
            if (agent.remainingDistance < 0.5f)
            {
                // 到达目标，等待一段时间后寻找新目标
                StartCoroutine(WaitAndFindNewDestination());
                hasTarget = false;
            }
        }

        // 更新滚动视觉效果
        if (enableRollingVisual && agent.velocity.magnitude > 0.1f)
        {
            UpdateRollingRotation();
        }
    }

    /// <summary>
    /// 在 NavMesh 上寻找随机目标点
    /// </summary>
    void FindRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * searchRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, searchRadius, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
            agent.SetDestination(targetPosition);
            hasTarget = true;
        }
        else
        {
            // 如果找不到有效位置，稍后重试
            StartCoroutine(WaitAndFindNewDestination());
        }
    }

    /// <summary>
    /// 等待一段时间后寻找新目标
    /// </summary>
    IEnumerator WaitAndFindNewDestination()
    {
        float waitTime = Random.Range(minWaitTime, maxWaitTime);
        yield return new WaitForSeconds(waitTime);
        FindRandomDestination();
    }

    /// <summary>
    /// 根据移动速度更新球的旋转，模拟滚动效果
    /// </summary>
    void UpdateRollingRotation()
    {
        if (agent.velocity.magnitude > 0.1f)
        {
            // 计算移动方向和旋转轴
            Vector3 moveDirection = agent.velocity.normalized;
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, moveDirection).normalized;

            // 计算旋转速度（基于移动速度和半径）
            float radius = transform.localScale.x * 0.5f; // 假设球体的半径
            float moveDistance = agent.velocity.magnitude * Time.deltaTime;
            float rotationAngle = (moveDistance / radius) * Mathf.Rad2Deg * rollingSpeedMultiplier;

            // 应用旋转
            transform.Rotate(rotationAxis, rotationAngle, Space.World);
        }
    }

    /// <summary>
    /// 在编辑器中可视化搜索半径
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        if (hasTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(targetPosition, 0.5f);
        }
    }
}
