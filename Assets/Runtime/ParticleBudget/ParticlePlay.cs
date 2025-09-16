using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ParticlePlay : MonoBehaviour
{
    // 基于一个 指定半径的球体，生成粒子
    [SerializeField] float radius = 1f;

    public void Play()
    {
        var pos = Random.insideUnitSphere * radius;
        var inst = ParticlePool.I.Spawn("fire", this.transform.position, this.transform.rotation);
    }

    void Start()
    {
        Play();
    }

}
