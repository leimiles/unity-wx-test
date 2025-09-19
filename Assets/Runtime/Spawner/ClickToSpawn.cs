using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ClickToSpawn : MonoBehaviour
{
    [SerializeField] GestureInput gestureInput;

    [SerializeField] Camera mainCamera;

    [SerializeField] GameObject spawnPrefab;

    void OnEnable()
    {
        gestureInput.onTapEvent += OnTap;
    }

    void OnDisable()
    {
        gestureInput.onTapEvent -= OnTap;
    }

    void OnTap(Vector2 pos)
    {
        var point = GetCollisionPointFromScreenPos(pos);

        if (point != null)
        {
            //ParticlePool.I.Spawn("P01", point, Quaternion.identity);
            //Debug.Log($"OnTap: {point}");
            var inst = Instantiate(spawnPrefab, point, Quaternion.identity);
            inst.transform.SetParent(transform);
        }
    }

    // 移动端性能友好
    Vector3 GetCollisionPointFromScreenPos(Vector2 pos)
    {
        var ray = mainCamera.ScreenPointToRay(pos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

}
