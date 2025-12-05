using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GeneralTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ModularCharSpawner.Instance.Spawn();
        ModularCharSpawner.Instance.Spawn();
        ModularCharSpawner.Instance.Spawn();
        ModularCharSpawner.Instance.Spawn();
    }

}
